//using SteamAudio;
//using UnityEngine;

//public class selectHRTF : MonoBehaviour
//{
//    private SteamAudioManager manager;
//    public GameObject gm;
//    public int crHRTF = 0;
//    // Start is called once before the first execution of Update after the MonoBehaviour is created
//    void Start()
//    {
//        manager = GetComponent<SteamAudioManager>();

//    }

//    // Update is called once per frame
//    void Update()
//    {
//        if (gm.GetComponent<DeformableSphere2>().isTouching)
//        {
//            SelectHRTF(crHRTF);
//            Debug.Log($"HRTF Selected = {crHRTF}");
//        }

//    }
//    void SelectHRTF(int cr)
//    {
//        manager.currentHRTF = cr;
//    }
//}