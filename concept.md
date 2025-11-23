# Multi-Modality in Virtual Reality

**Course:** EEL71010: Mobile AR and VR
**Student:** Ashish Kumar (M24AIR003)
**Report:** Progress Report-1

## 1. Objective

The project aims to integrate visual, haptic, and spatial audio feedback into a Virtual Reality Environment. The user is given a spherical ball in virtual reality to explore the surface of the ball. This project focuses on the following implementation:

*   **Real-time object deformation upon impact.**
*   **Haptic feedback for touch and recoil.**
*   **Spatial audio to generate sounds of deformation on the impact.**
*   **All of this will be visible and experienced through a head-mounted display.**

## 2. Problem Description

Traditional VR experiences primarily focus on visual immersion, often lacking realistic tactile feedback and spatial audio integration. This results in an incomplete sensory experience, reducing the overall realism and engagement of users. The project will create a multi-sensory, highly immersive VR environment by addressing these challenges.

## 3. Methodology

### 1. VR Environment Setup
*   Create a VR environment with a spherical object in front of the user.
*   The user will remain stationary and can look around the scene.
*   The user is provided with a haptic device that can give the user haptic feedback while exploring the object’s surface.

### 2. Implementation of Deformable Sphere
At the start of the simulation, the script captures the original mesh of the object:
*   `originalVerts`: Stores the original positions of all vertices.
*   `modifiedVerts`: Stores modified positions during deformation.
*   `velocity`: Tracks per-vertex velocity to simulate wave motion.

In each frame, the script checks the distance between the stylus and the object:
$$d = \|p_{stylus} - p_{object}\|$$

If $d < R_{proximity}$, deformation begins. The stylus position is converted to local coordinates to calculate a `hitPoint` for deformation.

**Wave-Based Deformation:**
For each vertex, a wave-based deformation is calculated as:
$$waveEffect = \sin(dist \cdot 2\pi) \cdot D_{max} \cdot e^{-2d}$$
where:
*   $dist$ is the distance between the vertex and the `hitPoint`.
*   $D_{max}$ is the maximum deformation amplitude.

The deformation is then applied in the direction of the wave and converted to local space for updating the mesh.

**Wave Simulation:**
Wave propagation is modeled using a spring-damping system:
$$a_i = -k(x_i - x_{0i}) - c \cdot v_i$$
where:
*   $k$ is the spring strength.
*   $c$ is the damping factor.
*   $x_i$ and $v_i$ are current position and velocity of vertex $i$.

**Mesh Reset:**
When the stylus exits the proximity radius, a coroutine is triggered to gradually reset the mesh to its original form:
$$x_i(t) = \text{Lerp}(x_i(t), x_{0i}, \alpha)$$

### 3. Haptic Feedback
*   Install and integrate the **Touch Device Driver for Haptics Direct** plugin into Unity [1, 2].
*   Establish communication between `TouchActor` (Virtual simulation of Touch Haptic Device via HapticsDirect) and Unity.
*   Provide a recoil sensation when the user pokes into the surface.

**Adding Haptic Feedback to the Sphere:**
*   Penetration depth $x$ is measured between the stylus tip and the sphere surface.
*   Restoring force is computed using:
    $$F = -kx$$
    where $k$ is the sphere’s stiffness.
*   The force is dynamically adjusted based on penetration depth and velocity.

**Adding Spring Force Feedback to the Sphere:**
A spring-based force response is simulated using Hooke’s Law. The force applied to the stylus is given by:
$$F = -k(x - x_0) - cv$$
where:
*   $x$ is the current penetration depth.
*   $x_0$ is the equilibrium position.
*   $c$ is a damping coefficient.
*   $v$ is the velocity of the stylus.

A force reduction mechanism ensures smooth transitions when the stylus exits the sphere to prevent abrupt changes in force feedback.

### 4. Spatial Audio Feedback
*   Installing and setting up **Steam Audio** for accurate 3D sound positioning.
*   Assign a unique spatial sound to an empty object triggered by collision.

### 5. Integration of Haptics, Visual, and Audio
*   Implement a **client-server application**.
*   The **Server** application runs in Unity (PC), whereas the **Client** runs in the HMD.
*   Same scene is created for the client as for the server but without haptics in the client.
*   Stylus position is communicated to the client, and the client updates its stylus position at each frame.
*   When a collision is detected at the client side, it places a sound source at the point of collision.
*   **Steam Audio** helps to spatialize the sound.
*   A custom SOFA dataset is given to Steam Audio. Steam Audio retrieves the head-related transfer impulses (HRIR) from the SOFA dataset, takes the parameters of head position, and interpolates the HRTF data (which is retrieved through convolution of HRIR and audio signal in frequency domain) using nearest neighbor technique. We also tested for a bilinear interpolation technique to render the HRTF sound in real time. The interpolation took place for every frame to create a perception of sound moving across the space.

## 4. Conclusion

By leveraging the Steam Audio Plugin and integrating custom scripts for collision detection and HRTF selection, the team has successfully implemented a realistic, interactive spatial audio system. This system ensures that sounds in the environment react dynamically to player interactions and movements, creating an immersive auditory experience. Further refinements in interpolation techniques and HRTF dataset selection may enhance the precision of sound localization, optimizing the overall quality of spatial audio rendering.

By employing client-server communication between HMD and PC, the team successfully implemented Multimodality in VR by incorporating haptics into the server PC and visual/audio into the HMD.

## Contribution

The team has been subdivided into two subgroups A and B.
*   **Team A:** Mihir Tomar and Subham Kushwaha (Integration of Haptics).
*   **Team B:** Ashish Kumar and Kompal Layal (Integration of Spatial Audio).

All the members in the subgroups contributed equally in their respective assignments. Team B was guided by Mr. Basant and Team A was guided by Mr. Trithankar Roy. Lastly, both teams collaborated to successfully implement the multimodality in VR.

## References

[1] 3D Systems. Haptics direct for unity v1. https://assetstore.unity.com/packages/tools/integration/haptics-direct-for-unity-v1-197034, Latest release date: March 2022.

[2] Nicolò Balzarotti and Gabriel Baud-Bovy. Hpge: an haptic plugin for game engines. In Games and Learning Alliance: 7th International Conference, GALA 2018, Palermo, Italy, December 5–7, 2018, Proceedings 7, pages 330–339. Springer, 2019.
