using UnityEngine;

namespace BadMovieClues.UI
{
    /// <summary>
    /// Persistent ambient background: a dedicated camera (depth -10, clear flags
    /// solid color clear to a dark purple fallback) plus a code-generated full-screen
    /// gradient quad (top dark purple to bottom deeper dark purple) and dual particle
    /// systems (soft cream "dust" drift + slow gold "sparkle" layer) for parallax depth.
    /// Built entirely in code (no prefab), parented under AppRoot to persist across scenes.
    /// Also the sole AudioListener in the app.
    /// </summary>
    public static class AmbientDustBackground
    {
        public struct DustSystems
        {
            public ParticleSystem Dust;
            public ParticleSystem Sparkle;
        }

        private static readonly Color BackgroundTopColor = new Color32(0x2A, 0x1A, 0x3E, 0xFF);
        private static readonly Color BackgroundBottomColor = new Color32(0x1A, 0x0E, 0x2E, 0xFF);
        private static readonly Color DustColor = new Color32(0xFF, 0xE9, 0xC2, 0xFF);
        private static readonly Color SparkleColor = new Color32(0xFF, 0xD7, 0x70, 0xFF);

        public static DustSystems Build(Transform parent)
        {
            // 1. Background Camera
            var cameraGo = new GameObject("BackgroundCamera", typeof(Camera), typeof(AudioListener));
            cameraGo.transform.SetParent(parent, false);
            cameraGo.transform.position = new Vector3(0f, 0f, -10f);
            cameraGo.tag = "MainCamera";

            var camera = cameraGo.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = BackgroundBottomColor;
            camera.depth = -10;

            // 2. Fullscreen Gradient Quad
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "BackgroundGradientQuad";
            quad.transform.SetParent(cameraGo.transform, false);
            // Position at local Z = 15 (world Z = 5) so it renders behind the particles at Z = 0
            quad.transform.localPosition = new Vector3(0f, 0f, 15f);
            // Sized generously to cover portrait viewports
            quad.transform.localScale = new Vector3(20f, 30f, 1f);

            var collider = quad.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.Destroy(collider);

            var meshRenderer = quad.GetComponent<MeshRenderer>();
            var shader = Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            var gradientTex = UITheme.CreateGradientTexture(BackgroundTopColor, BackgroundBottomColor, 256);
            mat.mainTexture = gradientTex;
            meshRenderer.sharedMaterial = mat;
            meshRenderer.sortingOrder = -200;

            // Load shared particle material
            var material = Resources.Load<Material>("DustParticleMaterial");

            // 3. Cream Dust Particle System
            var dustGo = new GameObject("DustParticles", typeof(ParticleSystem));
            dustGo.transform.SetParent(parent, false);
            dustGo.transform.position = Vector3.zero;

            var dustPs = dustGo.GetComponent<ParticleSystem>();
            SetupDustSystem(dustPs, DustColor, 50, 10f, new ParticleSystem.MinMaxCurve(8f, 14f), new ParticleSystem.MinMaxCurve(0.1f, 0.3f), new ParticleSystem.MinMaxCurve(0.15f, 0.5f), -0.02f, 0.35f, 0.08f, 0.2f, material, -100);

            // 4. Gold Sparkle Particle System
            var sparkleGo = new GameObject("SparkleParticles", typeof(ParticleSystem));
            sparkleGo.transform.SetParent(parent, false);
            sparkleGo.transform.position = Vector3.zero;

            var sparklePs = sparkleGo.GetComponent<ParticleSystem>();
            SetupDustSystem(sparklePs, SparkleColor, 20, 3f, new ParticleSystem.MinMaxCurve(12f, 20f), new ParticleSystem.MinMaxCurve(0.02f, 0.08f), new ParticleSystem.MinMaxCurve(0.3f, 0.8f), -0.01f, 0.15f, 0.05f, 0.1f, material, -99);

            return new DustSystems
            {
                Dust = dustPs,
                Sparkle = sparklePs
            };
        }

        private static void SetupDustSystem(
            ParticleSystem ps,
            Color color,
            int maxParticles,
            float rateOverTime,
            ParticleSystem.MinMaxCurve lifetime,
            ParticleSystem.MinMaxCurve speed,
            ParticleSystem.MinMaxCurve size,
            float gravity,
            float maxAlpha,
            float noiseStrength,
            float noiseFrequency,
            Material material,
            int sortingOrder)
        {
            var main = ps.main;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = maxParticles;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = size;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            main.startColor = color;
            main.gravityModifier = gravity;

            var emission = ps.emission;
            emission.rateOverTime = rateOverTime;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(10f, 14f, 1f);
            shape.randomDirectionAmount = 1f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(maxAlpha, 0.5f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = gradient;

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = noiseStrength;
            noise.frequency = noiseFrequency;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = sortingOrder;
            if (material != null) renderer.material = material;

            ps.Play();
        }

        /// <summary>Hook for the Settings "Reduced Effects" toggle - stops new particles spawning
        /// for both dust and sparkle systems, letting existing ones fade out naturally.</summary>
        public static void SetReducedEffects(DustSystems systems, bool reduced)
        {
            if (systems.Dust != null)
            {
                var emission = systems.Dust.emission;
                emission.enabled = !reduced;
            }
            if (systems.Sparkle != null)
            {
                var emission = systems.Sparkle.emission;
                emission.enabled = !reduced;
            }
        }
    }
}
