using UnityEngine;
using System.Collections;
using Unity.Networking.Transport;
using Unity.Collections;
using UnityEngine.Audio;

public class new_deform : MonoBehaviour
{
    NetworkDriver m_Driver;
    NetworkConnection m_Connection;
    private MeshFilter meshFilter;
    private Mesh mesh;
    private UnityEngine.Vector3[] originalVerts;
    private UnityEngine.Vector3[] modifiedVerts;
    private UnityEngine.Vector3[] velocity;
    public bool isTouching = false;
    public GameObject gm;
    private AudioSource audiosource;

    public Transform stylus;
    public Transform HapticStylys;
    public Rigidbody stylusRigidbody;
    public float waveSpeed = 6f;
    public float waveDamping = 0.92f;

    public float proximityRadius = 1.0f;
    public float maxDeformation = 0.3f;
    public float resetSpeed = 2f;
    public float springStrength = 8f;

    public float springConstant = 20f;
    public float dampingFactor = 5f;

    public UnityEngine.Vector3 lastForce = UnityEngine.Vector3.zero;
    public bool isExiting = false;

    private float contactTime = -1f;
    private float forceRenderTime = 0f;
    private bool contactRegistered = false;

    public string serverIP = "192.168.215.188";
    public ushort serverPort = 5000;

    Vector3 force;

    void Start()
    {
        m_Driver = NetworkDriver.Create();
        var endpoint = NetworkEndpoint.Parse(serverIP, serverPort);
        m_Connection = m_Driver.Connect(endpoint);
        Debug.Log("[CLIENT] Attempting to connect to server...");

        meshFilter = GetComponent<MeshFilter>();
        mesh = meshFilter.mesh;

        originalVerts = mesh.vertices;
        modifiedVerts = mesh.vertices;
        velocity = new UnityEngine.Vector3[originalVerts.Length];

        mesh.RecalculateBounds();
    }

    void OnDestroy()
    {
        m_Driver.Dispose();
    }

    IEnumerator ResetDeformation()
    {
        float resetTime = 1.0f;
        float elapsedTime = 0f;

        while (elapsedTime < resetTime)
        {
            for (int i = 0; i < modifiedVerts.Length; i++)
            {
                modifiedVerts[i] = UnityEngine.Vector3.Lerp(modifiedVerts[i], originalVerts[i], elapsedTime / resetTime);
                velocity[i] *= 0.5f;
            }

            elapsedTime += Time.deltaTime;
            ApplyMeshChanges();
            yield return null;
        }

        for (int i = 0; i < modifiedVerts.Length; i++)
        {
            modifiedVerts[i] = originalVerts[i];
            velocity[i] = UnityEngine.Vector3.zero;
        }

        ApplyMeshChanges();
    }

    void Update()
    {
        //Networking
        m_Driver.ScheduleUpdate().Complete();

        if (!m_Connection.IsCreated) return;

        DataStreamReader stream;
        NetworkEvent.Type cmd;
        while ((cmd = m_Connection.PopEvent(m_Driver, out stream)) != NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Connect)
            {
                Debug.Log("[CLIENT] Connected to server.");
            }
            else if (cmd == NetworkEvent.Type.Data)
            {
                float posX = stream.ReadFloat();
                float posY = stream.ReadFloat();
                float posZ = stream.ReadFloat();
                float rotX = stream.ReadFloat();
                float rotY = stream.ReadFloat();
                float rotZ = stream.ReadFloat();

                stylus.transform.position = Vector3.Lerp(stylus.transform.position, new Vector3(posX, posY, posZ), Time.deltaTime * 10);
                stylus.transform.rotation = Quaternion.Slerp(stylus.transform.rotation, Quaternion.Euler(rotX, rotY, rotZ), Time.deltaTime * 10);

                HapticStylys.transform.position = Vector3.Lerp(HapticStylys.transform.position, new Vector3(posX, posY, posZ), Time.deltaTime * 10);
                HapticStylys.transform.rotation = Quaternion.Slerp(HapticStylys.transform.rotation, Quaternion.Euler(rotX, rotY, rotZ), Time.deltaTime * 10);


                //Debug.Log($"[CLIENT] Received Stylus Data - Position: ({stylus.position}) Rotation: ({stylus.rotation.eulerAngles})");

                //deformableSphere.Update();

                //Logic
                if (stylus != null)
                {
                    float distance = UnityEngine.Vector3.Distance(stylus.position, transform.position);
                    if (distance < proximityRadius)
                    {
                        UnityEngine.Vector3 localHit = transform.InverseTransformPoint(stylus.position);
                        contactTime = Time.time;
                        ApplyWaveDeformation(localHit, distance);
                        ApplySpringForce(localHit, contactTime);
                        isTouching = true;
                        isExiting = false;
                        gm.transform.position = HapticStylys.position;
                        if (!audiosource.isPlaying)
                        {
                            audiosource.Play();
                        }
                        //Debug.Log(audiosource.name);
                        Debug.Log("Playing audio");
                    }
                    else if (isTouching)
                    {
                        isTouching = false;
                        isExiting = true;
                        audiosource.Pause();
                        StartCoroutine(ResetDeformation());
                    }
                }

                SimulateWaveMotion();
                ApplyMeshChanges();


                ////Vector3 forceFeedback = deformableSphere.lastForce;
                //SendForceFeedback(forceFeedback);
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                Debug.Log("[CLIENT] Disconnected from server.");
                m_Connection = default;
            }
        }




    }

    void SendForceFeedback(Vector3 force, float ctime)
    {
        m_Driver.BeginSend(NetworkPipeline.Null, m_Connection, out var writer);
        writer.WriteFloat(force.x);
        writer.WriteFloat(force.y);
        writer.WriteFloat(force.z);
        writer.WriteFloat(ctime);
        m_Driver.EndSend(writer);

        Debug.Log($"[CLIENT] Sent Force Feedback - ({force.magnitude})N, Direction: {force.normalized}");
    }

    void ApplySpringForce(UnityEngine.Vector3 hitPoint, float ctime)
    {
        if (stylusRigidbody == null) return;

        UnityEngine.Vector3 displacement = transform.position - stylus.position;
        float penetrationDepth = displacement.magnitude;

        if (penetrationDepth > 0.001f)
        {
            isExiting = false;
            force = (-springConstant * displacement) - (dampingFactor * stylusRigidbody.linearVelocity);

            float fadeFactor = Mathf.Clamp01((proximityRadius - penetrationDepth) / proximityRadius);
            force *= fadeFactor;

            stylusRigidbody.AddForce(force, ForceMode.Force);
            lastForce = -force;

            forceRenderTime = Time.time;

            //float timeLag = (forceRenderTime - contactTime) * 1000f;
            /*Debug.Log($"Force Applied: {force}, Magnitude: {force.magnitude}, Direction: {force.normalized}");
            Debug.Log($"Force Feedback: {lastForce}, Magnitude: {lastForce.magnitude}, Direction: {lastForce.normalized}");
            Debug.Log($"Time Lag: {timeLag} ms");*/
        }
        else if (isExiting)
        {
            stylusRigidbody.AddForce(lastForce * 0.98f, ForceMode.Force);
            lastForce *= -0.98f;

            if (lastForce.magnitude < 0.01f)
            {
                lastForce = UnityEngine.Vector3.zero;
                isExiting = false;
            }
        }
        else
        {
            contactRegistered = false;
            contactTime = -1f;
        }
        //Vector3 forceFeedback = deformableSphere.lastForce;
        SendForceFeedback(lastForce, ctime);
    }

    void ApplyMeshChanges()
    {
        mesh.vertices = modifiedVerts;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
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
}