using UnityEngine;
using System.Collections;

public class ogdeform : MonoBehaviour
{

    private MeshFilter meshFilter;
    private Mesh mesh;
    private UnityEngine.Vector3[] originalVerts;
    private UnityEngine.Vector3[] modifiedVerts;
    private UnityEngine.Vector3[] velocity;
    public GameObject gm;
    private AudioSource audiosource;
    public bool isTouching = false;

    public Transform stylus; // Assign the stylus GameObject in the Inspector
    public Transform HapticStylys; // Assign the stylus GameObject in the Inspector
    public Rigidbody stylusRigidbody; // Assign the stylus' Rigidbody for force feedback
    public Rigidbody ColliderRigidbody; // Assign the stylus' Rigidbody for force feedback

    public float proximityRadius = 1.0f; // How close before it starts deforming
    public float maxDeformation = 0.3f; // Maximum depth of deformation
    public float resetSpeed = 2f; // How quickly it resets
    public float waveSpeed = 6f; // Speed of wave propagation
    public float waveDamping = 0.92f; // Reduces wave energy over time
    public float springStrength = 8f; // How bouncy the deformation is

    public float springConstant = 20f; // Stiffness of force feedback (higher = stronger pushback)
    public float dampingFactor = 5f; // Controls smoothness (higher = less bouncing)

    private UnityEngine.Vector3 lastForce = UnityEngine.Vector3.zero; // Store last force to smoothly blend out
    private bool isExiting = false; // Track whether the stylus is leaving the sphere

    public float contactTime = -1f; // Tracks the time of first contact
    public float audioplaytime = -1f; // Time when force is rendered
    private bool contactRegistered = false; // To track if contact was already registered
    private float forceRenderTime = -1f;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();

        mesh = meshFilter.mesh;
        audiosource = gm.GetComponent<AudioSource>();

        originalVerts = mesh.vertices;
        modifiedVerts = mesh.vertices;
        velocity = new UnityEngine.Vector3[originalVerts.Length];

        mesh.RecalculateBounds(); // Ensure valid bounds at start
    }

    IEnumerator ResetDeformation()
    {
        float resetTime = 1.0f; // Duration to fully reset
        float elapsedTime = 0f;

        while (elapsedTime < resetTime)
        {
            for (int i = 0; i < modifiedVerts.Length; i++)
            {
                modifiedVerts[i] = UnityEngine.Vector3.Lerp(modifiedVerts[i], originalVerts[i], elapsedTime / resetTime);
                velocity[i] *= 0.5f; // Dampen the velocity to stop lingering movement
            }

            elapsedTime += Time.deltaTime;
            ApplyMeshChanges();
            yield return null;
        }

        // Ensure full reset
        for (int i = 0; i < modifiedVerts.Length; i++)
        {
            modifiedVerts[i] = originalVerts[i];
            velocity[i] = UnityEngine.Vector3.zero;
        }

        ApplyMeshChanges();
    }

    void Update()
    {


        if (stylus != null)
        {
            float distance = UnityEngine.Vector3.Distance(HapticStylys.position, transform.position);
            if (distance < proximityRadius)
            {
                isTouching = true;
                contactTime = Time.time;
                UnityEngine.Vector3 localHit = transform.InverseTransformPoint(HapticStylys.position);
                ApplyWaveDeformation(localHit, distance);
                //ApplyLocalizedDeformation(localHit, distance);
                ApplySpringForce(localHit);
                isExiting = false;
                gm.transform.position = localHit;
                if (!audiosource.isPlaying)
                {
                    audiosource.Play();
                    audioplaytime = Time.time;
                    //Debug.Log($"AT:{audioplaytime}");
                    //Debug.Log($"CT:{contactTime}");
                    //Debug.Log($"Audio Play lag:{contactTime - audioplaytime}");
                }
                //Debug.Log(audiosource.name);
                //Debug.Log("Playing audio");

                //contactTime = Time.time;
            }
            else if (isTouching)
            {
                isTouching = false;
                isExiting = true;
                audiosource.Pause();
                Debug.Log("Not Playing audio");

                StartCoroutine(ResetDeformation()); // Smoothly reset sphere
            }
        }

        SimulateWaveMotion();
        //SimulateDeformationMotion();
        ApplyMeshChanges();
    }

    //void ApplyLocalizedDeformation(Vector3 hitPoint, float distance)
    //{
    //    float maxInfluenceRadius = 0.5f;
    //    float penetrationDepth = Mathf.Clamp01((proximityRadius - distance) / proximityRadius);
    //    float deformationAmount = maxDeformation * penetrationDepth;

    //    for (int i = 0; i < modifiedVerts.Length; i++)
    //    {
    //        float distToHit = Vector3.Distance(originalVerts[i], hitPoint);

    //        if (distToHit < maxInfluenceRadius)
    //        {
    //            float falloff = 1.0f - (distToHit / maxInfluenceRadius);
    //            Vector3 direction = (originalVerts[i] - hitPoint).normalized;
    //            Vector3 deformation = -direction * deformationAmount * falloff;

    //            velocity[i] += deformation * 10f;
    //        }
    //    }
    //}
    //void SimulateDeformationMotion()
    //{
    //    for (int i = 0; i < modifiedVerts.Length; i++)
    //    {
    //        Vector3 displacement = modifiedVerts[i] - originalVerts[i];
    //        velocity[i] -= displacement * springStrength * Time.deltaTime;
    //        velocity[i] *= waveDamping;
    //        modifiedVerts[i] += velocity[i] * Time.deltaTime * waveSpeed;

    //        if (float.IsNaN(modifiedVerts[i].x) || float.IsNaN(modifiedVerts[i].y) || float.IsNaN(modifiedVerts[i].z))
    //        {
    //            Debug.LogError("NaN detected in vertex deformation! Resetting vertex.");
    //            modifiedVerts[i] = originalVerts[i];
    //            velocity[i] = Vector3.zero;
    //        }
    //    }
    //}
    void ApplyWaveDeformation(UnityEngine.Vector3 hitPoint, float distance)
    {
        float proximityEffect = Mathf.Exp(-distance * 2.0f);
        UnityEngine.Vector3 deformationDirection = (hitPoint - transform.position).normalized;

        for (int i = 0; i < modifiedVerts.Length; i++)
        {
            UnityEngine.Vector3 worldVertex = transform.TransformPoint(originalVerts[i]);
            float dist = UnityEngine.Vector3.Distance(hitPoint, worldVertex);
            float waveEffect = Mathf.Sin(dist * Mathf.PI * 2.0f) * maxDeformation * proximityEffect;
            UnityEngine.Vector3 deformation = deformationDirection * waveEffect;
            UnityEngine.Vector3 targetVertex = worldVertex + deformation;

            velocity[i] += (transform.InverseTransformPoint(targetVertex) - modifiedVerts[i]) * 10f;
        }
    }

    void ApplySpringForce(UnityEngine.Vector3 hitPoint)
    {
        if (stylusRigidbody == null) return;

        UnityEngine.Vector3 displacement = transform.position - stylus.position;
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
            //Debug.Log($"Force Feedback: {lastForce}, Magnitude: {lastForce.magnitude}, Direction: {lastForce.normalized}");
            Debug.Log($"Force Time Lag: {timeLag} ms");


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

    void SimulateWaveMotion()
    {
        for (int i = 0; i < modifiedVerts.Length; i++)
        {
            UnityEngine.Vector3 displacement = modifiedVerts[i] - originalVerts[i];
            velocity[i] -= displacement * springStrength * Time.deltaTime; // Spring effect
            velocity[i] *= waveDamping; // Damping effect
            modifiedVerts[i] += velocity[i] * Time.deltaTime * waveSpeed;

            // 🔹 Clamp extreme deformations to prevent AABB errors
            if (float.IsNaN(modifiedVerts[i].x) || float.IsNaN(modifiedVerts[i].y) || float.IsNaN(modifiedVerts[i].z))
            {
                Debug.LogError("NaN detected in vertex deformation! Resetting vertex.");
                modifiedVerts[i] = originalVerts[i];
                velocity[i] = UnityEngine.Vector3.zero;
            }
        }
    }

    void ApplyMeshChanges()
    {
        mesh.vertices = modifiedVerts;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
}