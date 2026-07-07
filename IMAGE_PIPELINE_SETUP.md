# Offline "Bad Art" Image Pipeline — Setup Guide

Runs entirely in free cloud notebooks. Nothing here ever ships in the app —
this is a dev-time tool only, matching the "no image generation in the
shipped game" rule in `CLAUDE.md`. Independent of the Unity milestones; do
this whenever, in parallel.

## Step 1 — Try generation first (no LoRA yet)

Open **ComfyUI on Colab** (official notebook maintained by ltdrdata, the
ComfyUI-Manager author):
https://colab.research.google.com/github/ltdrdata/ComfyUI-Manager/blob/main/notebooks/comfyui_colab_with_manager.ipynb

1. `Runtime > Change runtime type` → GPU (T4, free tier).
2. Run all cells top to bottom. First boot takes ~5–10 min.
3. It prints a public URL (Cloudflare/gradio tunnel) — open that for the full
   ComfyUI drag-and-drop interface.
4. Inside ComfyUI, use the **Manager** button → install/download an SDXL
   checkpoint (search "SDXL base 1.0", i.e. `sd_xl_base_1.0.safetensors`).
5. Try prompting your style in plain text first, no LoRA: something like
   *"crude child's crayon and biro sketch, messy scribbly lines, [scene
   description]"*. This tells you the gap a style LoRA needs to close.

Alternative notebook if that one is ever down/stale:
https://github.com/nazdridoy/ComfyUI-On-Colab

## Step 2 — Prepare the style-LoRA training set

Use the images already in `Assets/_Project/Content/Images/` (all ~9 of them,
including the two `_unmatched_*` ones — a *style* LoRA learns the look, not
the content, so movie-matching doesn't matter here).

For each image, make a caption `.txt` file with the same base name, describing
the content plainly and ending with one consistent made-up trigger word, e.g.:

```
img_jaws.jpg          -> img_jaws.txt:          "a shark above a small boat in the ocean, bmcstyle"
img_et.jpg            -> img_et.txt:            "an alien pointing next to a bicycle, bmcstyle"
img_matrix.png        -> img_matrix.txt:         "a man in sunglasses dodging bullets in a city, bmcstyle"
```

Pick your own trigger word once (e.g. `bmcstyle`) and use it in every caption —
that's the token you'll put in prompts later to invoke the look. Zip the
images + `.txt` files together and upload the zip to Google Drive (the
training notebook reads from Drive).

**Honest expectation:** the community-vetted sweet spot for a LoRA is ~20–25
images; you have ~9. Treat this first LoRA as a v1 — usable, but a looser
style-lock than ideal. Retrain later as you curate more art (keep adding to
the same Drive folder).

## Step 3 — Train the LoRA

Use one of these actively-maintained Kohya-based SDXL LoRA Colab notebooks:
- https://github.com/hollowstrawberry/kohya-colab (accessible, well-documented)
- https://colab.research.google.com/github/Linaqruf/kohya-trainer/blob/main/kohya-LoRA-trainer-XL.ipynb

Point it at the SDXL base checkpoint and your zipped dataset. Settings for a
small ~9-image set:
- **Repeats:** 15–20 per image (small datasets need more repeats/epoch than
  the notebook's defaults assume)
- **Epochs:** ~10
- **Network dim / alpha:** moderate, e.g. 32 / 16
- **Resolution:** 1024 (SDXL native)

Expect ~30–60 min training time on a free T4 for this dataset size. Output is
a single `.safetensors` file — download it.

**If Colab disconnects before training finishes:** switch to **Kaggle**
instead — it gives ~30 free GPU-hours/week with longer, more predictable
session limits than Colab's free tier, and the same Kohya scripts run there.
Search "Kohya SDXL LoRA Kaggle notebook" if you hit this.

## Step 4 — Generate with the trained LoRA

Back in ComfyUI: load the SDXL checkpoint, add a `LoraLoader` node pointing at
your downloaded `.safetensors`, and prompt using your trigger word (e.g.
`bmcstyle`) plus a plain description of the new movie scene.

- Batch-generate several candidates per movie (vary the seed).
- Curate: keep the funniest/clearest, reject the rest.
- Download keepers, rename to the project convention `img_<movie-slug>.jpg`
  (matching `id` in `Assets/_Project/Content/StarterCatalog.json`), and drop
  them into `Assets/_Project/Content/Images/`.

## Step 5 — Wiring into the game

This is a manual JSON edit for now — the in-editor curation tool comes in
milestone M1 of the main plan. Once M1 exists, you'll assign images to
catalog entries visually instead.

## Sources
- [Run and Share ComfyUI on Google Colab for Free](https://pinggy.io/blog/run_and_share_comfyui_on_google_colab/)
- [comfyui_colab_with_manager.ipynb (ltdrdata/ComfyUI-Manager)](https://colab.research.google.com/github/ltdrdata/ComfyUI-Manager/blob/main/notebooks/comfyui_colab_with_manager.ipynb)
- [ComfyUI-On-Colab (nazdridoy)](https://github.com/nazdridoy/ComfyUI-On-Colab)
- [kohya-colab (hollowstrawberry)](https://github.com/hollowstrawberry/kohya-colab)
- [kohya-LoRA-trainer-XL.ipynb (Linaqruf/kohya-trainer)](https://colab.research.google.com/github/Linaqruf/kohya-trainer/blob/main/kohya-LoRA-trainer-XL.ipynb)
- [How to Do SDXL Training For FREE with Kohya LoRA - Kaggle](https://civitai.com/articles/2090/how-to-do-sdxl-training-for-free-with-kohya-lora-kaggle-no-gpu-required-pwns-google-colab)
