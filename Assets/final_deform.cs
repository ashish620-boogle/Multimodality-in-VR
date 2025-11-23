using UnityEngine;
using System.Collections;

public class final_deform : MonoBehaviour
{
    private MeshFilter meshFilter;
    private Mesh mesh;
    private Vector3[] originalVerts;
    private Vector3[] modifiedVerts;
    private Vector3[] velocity;
    public GameObject gm;
    private AudioSource audiosource;
    public bool isTouching = false;

    public Transform stylus; // Assign the stylus GameObject in the Inspector
    public Transform HapticStylus; // Assign the stylus GameObject in the Inspector
    public Rigidbody stylusRigidbody; // Assign the stylus' Rigidbody for force feedback
    public Rigidbody HapticStylusRigidbody; // Assign the stylus' Rigidbody for force feedback

    public float proximityRadius = 1.0f; // How close before it starts deforming
    public float maxDeformation = 0.3f; // Maximum depth of deformation
    public float resetSpeed = 2f; // How quickly it resets
    public float waveSpeed = 6f; // Speed of wave propagation
    public float waveDamping = 0.92f; // Reduces wave energy over time
    public float springStrength = 8f; // How bouncy the deformation is

    public float springConstant = 20f; // Stiffness of force feedback (higher = stronger pushback)
    public float dampingFactor = 5f; // Controls smoothness (higher = less bouncing)

    private Vector3 lastForce = Vector3.zero; // Store last force to smoothly blend out
    private bool isExiting = false; // Track whether the stylus is leaving the sphere

    public float contactTime = -1f; // Tracks the time of first contact
    private float forceRenderTime = 0f; // Time when force is rendered
    private bool contactRegistered = false; // To track if contact was already registered

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        mesh = meshFilter.mesh;
        audiosource = gm.GetComponent<AudioSource>();

        originalVerts = mesh.vertices;
        modifiedVerts = mesh.vertices;
        velocity = new Vector3[originalVerts.Length];

        mesh.RecalculateBounds(); // Ensure valid bounds at start
    }

    IEnumerator ResetDeformation()
    {
        float resetTime = 1.0f;
        float elapsedTime = 0f;

        while (elapsedTime < resetTime)
        {
            for (int i = 0; i < modifiedVerts.Length; i++)
            {
                modifiedVerts[i] = Vector3.Lerp(modifiedVerts[i], originalVerts[i], elapsedTime / resetTime);
                velocity[i] *= 0.5f;
            }

            elapsedTime += Time.deltaTime;
            ApplyMeshChanges();
            yield return null;
        }

        for (int i = 0; i < modifiedVerts.Length; i++)
        {
            modifiedVerts[i] = originalVerts[i];
            velocity[i] = Vector3.zero;
        }

        ApplyMeshChanges();
    }

    private bool wasAudioPlaying = false;  // Add this at the top with other variables

    void Update()
    {
        if (HapticStylus != null)
        {
            float distance = Vector3.Distance(HapticStylus.position, transform.position);
            if (distance < proximityRadius)
            {
                Vector3 localHit = transform.InverseTransformPoint(HapticStylus.position);
                ApplyLocalizedDeformation(localHit, distance);
                ApplySpringForce(localHit);
                isTouching = true;
                contactTime = Time.time;
                isExiting = false;
                gm.transform.position = localHit;

                if (!audiosource.isPlaying)
                {
                    audiosource.Play();
                }

                if (!wasAudioPlaying)
                {
                    Debug.Log("Playing audio");
                    wasAudioPlaying = true;
                }
            }
            else if (isTouching)
            {
                isTouching = false;
                isExiting = true;

                if (audiosource.isPlaying)
                {
                    audiosource.Pause();
                }

                if (wasAudioPlaying)
                {
                    Debug.Log("Not Playing audio");
                    wasAudioPlaying = false;
                }

                StartCoroutine(ResetDeformation());
            }
        }

        SimulateDeformationMotion();
        ApplyMeshChanges();
    }


    void ApplyLocalizedDeformation(Vector3 hitPoint, float distance)
    {
        float maxInfluenceRadius = 0.5f;
        float penetrationDepth = Mathf.Clamp01((proximityRadius - distance) / proximityRadius);
        float deformationAmount = maxDeformation * penetrationDepth;

        for (int i = 0; i < modifiedVerts.Length; i++)
        {
            float distToHit = Vector3.Distance(originalVerts[i], hitPoint);

            if (distToHit < maxInfluenceRadius)
            {
                float falloff = 1.0f - (distToHit / maxInfluenceRadius);
                Vector3 direction = (originalVerts[i] - hitPoint).normalized;
                Vector3 deformation = -direction * deformationAmount * falloff;

                velocity[i] += deformation * 10f;
            }
        }
    }


    void SimulateDeformationMotion()
    {
        for (int i = 0; i < modifiedVerts.Length; i++)
        {
            Vector3 displacement = modifiedVerts[i] - originalVerts[i];
            velocity[i] -= displacement * springStrength * Time.deltaTime;
            velocity[i] *= waveDamping;
            modifiedVerts[i] += velocity[i] * Time.deltaTime * waveSpeed;

            if (float.IsNaN(modifiedVerts[i].x) || float.IsNaN(modifiedVerts[i].y) || float.IsNaN(modifiedVerts[i].z))
            {
                Debug.LogError("NaN detected in vertex deformation! Resetting vertex.");
                modifiedVerts[i] = originalVerts[i];
                velocity[i] = Vector3.zero;
            }
        }
    }

    void ApplySpringForce(UnityEngine.Vector3 hitPoint)
    {
        if (HapticStylusRigidbody == null) return;

        UnityEngine.Vector3 displacement = transform.position - HapticStylus.position;
        float penetrationDepth = displacement.magnitude; // Deeper penetration = stronger force

        if (penetrationDepth > 0.001f) // Apply force only while pressing in
        {

            isExiting = false; // Reset exit flag if stylus is inside
            UnityEngine.Vector3 force = (-springConstant * displacement) - (dampingFactor * stylusRigidbody.linearVelocity);

            // Smoothly fade force out near boundary instead of an abrupt stop
            float fadeFactor = Mathf.Clamp01((proximityRadius - penetrationDepth) / proximityRadius);
            force *= fadeFactor;

            stylusRigidbody.AddForce(force, ForceMode.Force);
            lastForce = -force; // Store the last applied force

            // Record the time of force rendering
            forceRenderTime = Time.time;

            // Calculate and log the time lag (in milliseconds)
            float timeLag = (forceRenderTime - contactTime) * 1000f; // Convert to ms
            Debug.Log($"Force Applied: {force}, Magnitude: {force.magnitude}, Direction: {force.normalized}");
            //Debug.Log($"Force Applied: {}");
            Debug.Log($"Force Feedback: {lastForce}, Magnitude: {lastForce.magnitude}, Direction: {lastForce.normalized}");
            Debug.Log($"Time Lag: {timeLag} ms");


        }
        else if (isExiting) // If stylus is leaving the sphere
        {
            stylusRigidbody.AddForce(lastForce * 0.98f, ForceMode.Force); // Gradually reduce force
            lastForce *= -0.98f; // Fade force out smoothly

            if (lastForce.magnitude < 0.01f) // Stop applying force when it's very small
            {
                lastForce = UnityEngine.Vector3.zero;
                isExiting = false;
            }
        }
        else
        {
            // Reset contact time and state when not in contact
            contactRegistered = false;
            contactTime = -1f; // Reset the contact time
        }
    }

    void ApplyMeshChanges()
    {
        mesh.vertices = modifiedVerts;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
}