# CLAIRE

**DLP-I516-213**  
Gilead Life Sciences  

## Overview

**CLAIRE** is an AI-driven image analysis platform designed to support monoclonal cell line identification. It leverages machine learning and advanced imaging techniques to automatically classify cellular content in well-plate images, including single cells, multiple cells, debris, and contaminants.

The platform tracks cell growth over time to verify monoclonality and analyzes both brightfield and fluorescence images to improve classification accuracy. Based on this analysis, CLAIRE automatically generates a hit-pick list that can be directly ingested by robotic cell-picking systems, significantly streamlining downstream operations.

---

## Table of Contents

- System Requirements
  - Windows OS
  - macOS (Docker)
- Getting Started
  - Windows
  - macOS (Docker)
- Supported Image Formats & Requirements
- Processing Time Estimates
- Detailed Runtime Analysis
- Output Location & Results
- Upload Folder Structure
- Pre-Run Checklist
- FAQs

---

## System Requirements

### Windows OS

| Component | Minimum Requirements | Recommended Requirements |
|---------|---------------------|--------------------------|
| **CPU** | Intel Core i5 (8th gen) or AMD Ryzen 5 (4 cores / 8 threads) | Intel Core i7/i9 (8th gen+) or AMD Ryzen 7/9 (8+ cores) |
| **RAM** | 16 GB | 32 GB |
| **Storage** | 50 GB free | 100+ GB free |
| **GPU** | CPU-only (default if no GPU detected) | NVIDIA GPU (4 GB+ VRAM, CUDA-compatible) |

**Execution (Windows):**  
Launch `Claire.exe` to open the GUI.

---

### macOS (Docker)

| Component | Minimum Requirements | Recommended Requirements |
|---------|---------------------|--------------------------|
| **CPU** | Apple M1/M2 (8+ cores) or Intel i7 (8th gen+) | M1 Pro / M2 Pro or Intel i9 |
| **RAM** | 16 GB | 32 GB |
| **Storage** | 50 GB free | 100+ GB free |
| **GPU** | Not supported (CPU-only) | Not supported (CPU-only) |

#### macOS GPU Notes
- macOS does **not** support NVIDIA CUDA GPUs
- Apple Silicon GPUs require MPS backend support
- Current implementation runs **CPU-only** on macOS

---

## Getting Started

### Windows

1. Download `Claire.exe`
2. Double-click to launch the GUI
3. Click **Choose Folder** and select your project directory
4. Wait for analysis to complete
5. Results are saved to your **Downloads** folder

---

### macOS (Docker)

1. Unzip the provided package (contains the `claire` folder)
2. Install Docker Desktop from https://www.docker.com/products/docker-desktop
3. Launch Docker Desktop and ensure it is running
4. Open Terminal and navigate to the `claire` directory:
   ```bash
   cd /path/to/claire
   ```
5. Build the Docker image:
   ```bash
   docker build -t claire .
   ```
6. Run CLAIRE:
   ```bash
   ./run_claire.sh /path/to/your/data/folder
   ```
7. Results will be available in:
   ```
   claire/claire_results
   ```

---

## Supported Image Formats & Requirements

### Supported Formats
- **JPEG** (`.jpg`, `.jpeg`)
- **PNG** (`.png`)

### Image Requirements
- No embedded annotations or overlays
- Consistent resolution across images
- Grayscale or RGB

### Image Capture Timeline
- **Day 0**
- **Day 14**

Each well must contain **exactly two images**:
- Brightfield
- Fluorescence

---

## Processing Time Estimates

Average runtime for one **96-well plate** (2–3 images per well):

- **Recommended system:** 30–45 minutes
- **Minimum system:** 60–90 minutes

---

## Detailed Runtime Analysis

### Performance Estimates

| Configuration | Total Time | Per Well | Notes |
|--------------|-----------|----------|------|
| Minimum | 60–90 min | 40–60 sec | CPU-only |
| Recommended | 30–45 min | 20–30 sec | CPU-only |
| With GPU | 10–20 min | 6–12 sec | Windows only |

### Current Limitations
- Sequential well processing
- Single-threaded prediction
- GPU acceleration not enabled by default

---

## Output Location & Results

Generated outputs include:
- Processed images
- Analysis results
- Patch bounding boxes
- Brightfield images (Day 0 & Day 14)
- Fluorescence images (Day 0)

---

## Upload Folder Structure

```
ProjectFolder/
├── 96W1_Day0_Ch1/
│   ├── Well_A1_Ch1.png
│   ├── Well_A1_Ch2.png
│   └── ...
├── 96W1_Day14_Ch1/
│   ├── Well_A1_Ch1.png
│   └── ...
└── 96W2_Day0_Ch1/
    └── ...
```

---

## Pre-Run Checklist

- Correct folder structure
- JPEG or PNG images only
- Image size within supported range
- Minimum 50 GB free disk space
- Day 0 and Day 14 data present

---

## FAQs

**Q: Why do I see “No project directory found”?**  
Ensure folder naming follows the required pattern.

**Q: Why is processing slow?**  
Close other applications to free CPU and RAM.

---

**Version:** 1.0  
**Last Updated:** February 2, 2026
