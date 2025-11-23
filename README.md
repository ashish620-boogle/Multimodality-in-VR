# Multimodality in VR

This project explores multimodality in Virtual Reality, focusing on the integration of haptics and spatial audio to enhance user immersion.

## Overview

The "Multimodality-in-VR" project investigates how combining different sensory modalities—specifically haptics and spatial audio—can create a more persuasive sense of presence in VR environments.

## Implementation Details

### 1. Initial Phase: Separate Setup
The project began by implementing and validating the two core sensory modalities independently:
*   **Haptics:** Setup and calibration of the **Omni haptic device** to provide precise tactile feedback.
*   **Spatial Audio:** Integration of **Steam Audio** to enable realistic sound propagation, occlusion, and spatialization.

### 2. Integration Strategy: TCP Networking
A significant architectural challenge was the physical separation of the hardware components. The Head Mounted Display (HMD) is designed to handle vision and spatial audio, while the Omni haptic device operates as a distinct peripheral.

To unify these systems, we implemented a **TCP networking** solution:
*   **HMD (Client):** Responsible for the visual rendering and spatial audio processing. It detects user interactions within the virtual environment.
*   **Omni Device (Server):** Dedicated to controlling the haptic feedback mechanisms.
*   **Synchronization:** The HMD communicates interaction events (e.g., touching a virtual object) via TCP to the Omni controller. This ensures that the haptic feedback is perfectly synchronized with the visual and auditory cues, creating a cohesive multimodal experience.

## Project Report

For a detailed conceptual overview and report on this project, please refer to the included PDF:
[**prr.pdf**](./prr.pdf)

## Getting Started

### Prerequisites

*   **Unity Version:** 6000.0.34f1
*   **Platforms:** Android (Oculus/Meta Quest), PC Standalone
*   **Hardware:** VR Headset (Oculus/Meta Quest), Omni Haptic Device

### Installation

1.  Clone the repository:
    ```bash
    git clone https://github.com/ashish620-boogle/Multimodality-in-VR.git
    ```
2.  Open the project in Unity Hub.
3.  Ensure all dependencies (Steam Audio, etc.) are installed via the Package Manager.

## Structure

*   `Assets/`: Contains all source code, scenes, and resources.
*   `prr.pdf`: Project Report.
*   `haptic_app.apk`: Pre-built Android package (if applicable).

## License

[License Information]
