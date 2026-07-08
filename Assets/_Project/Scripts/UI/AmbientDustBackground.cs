using UnityEngine;

namespace BadMovieClues.UI
{
    /// <summary>
    /// Persistent ambient background: a dedicated camera (depth -10, solid
    /// color clear) plus a soft "dust" particle drift, both parented under
    /// AppRoot so they render behind every scene's Screen Space Overlay UI
    /// with zero per-scene duplication. Built entirely in code (no prefab),
    /// matching AppRoot's own no-scene-placement construction from M8.
    /// Also the sole AudioListener in the app - the per-scene cameras that
    /// used to carry one were removed since Screen Space Overlay UI never
    /// needed a scene camera in the first place.
    /// </summary>
    public static class AmbientDustBackground
    {
        private static readonly Color BackgroundColor = new Color32(0x2A, 0x1A, 0x3E, 0xFF);
        private static readonly Color ParticleColor = new Color32(0xFF, 0xE9, 0xC2, 0xFF);

        public static ParticleSystem Build(Transform parent)
        {
            var cameraGo = new GameObject("BackgroundCamera", typeof(Camera), typeof(AudioListener));
            cameraGo.transform.SetParent(parent, false);
            cameraGo.transform.position = new Vector3(0f, 0f, -10f);
            cameraGo.tag = "MainCamera";

            var camera = cameraGo.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = BackgroundColor;
            camera.depth = -10;

            var particleGo = new GameObject("DustParticles", typeof(ParticleSystem));
            particleGo.transform.SetParent(parent, false);
            particleGo.transform.position = Vector3.zero;

            var ps = particleGo.GetComponent<ParticleSystem>();

            var main = ps.main;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 50;
            main.startLifetime = new ParticleSystem.MinMaxCurve(8f, 14f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.5f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            main.startColor = ParticleColor;
            main.gravityModifier = -0.02f;

            var emission = ps.emission;
            emission.rateOverTime = 10f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(10f, 14f, 1f);
            shape.randomDirectionAmount = 1f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(ParticleColor, 0f), new GradientColorKey(ParticleColor, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.35f, 0.5f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = gradient;

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.08f;
            noise.frequency = 0.2f;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = -100;
            // A ParticleSystem added via AddComponent (not the Editor's
            // GameObject > Effects menu) gets no material at all, which
            // renders as Unity's magenta "missing shader" fallback - a hard
            // solid square, not a soft blob. DustParticleMaterial is a real
            // committed asset (Sprites/Default + a soft radial-alpha PNG)
            // specifically so its shader survives IL2CPP build stripping -
            // Resources.GetBuiltinResource<Material>("Default-Particle.mat")
            // would still be at the mercy of whatever shader Unity picks
            // internally being stripped if nothing else references it.
            var material = Resources.Load<Material>("DustParticleMaterial");
            if (material != null) renderer.material = material;

            ps.Play();
            return ps;
        }

        /// <summary>Hook for the future Settings "Reduced Effects" toggle - just
        /// stops new particles spawning, existing ones fade out naturally.</summary>
        public static void SetReducedEffects(ParticleSystem dustSystem, bool reduced)
        {
            if (dustSystem == null) return;
            var emission = dustSystem.emission;
            emission.enabled = !reduced;
        }
    }
}
