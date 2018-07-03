using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class AudioWebGL : MonoBehaviour
{
    [DllImport("__Internal")]
    static extern void AudioInit();

    [DllImport("__Internal")]
    static extern void AudioStart();

    [DllImport("__Internal")]
    static extern void AudioStop();

    [DllImport("__Internal")]
    static extern void AudioClear();

    //[DllImport("_Internal")]
    //static extern int GetLength(int a, int b);

    [DllImport("__Internal")]
    static extern float GetAudioData(int index);

    [DllImport("__Internal")]
    private static extern int getLength();
    private float[] audioData;

    private int _length;

    private int _count;

    public AudioSource Audio;
    // Use this for initialization
    public void Init()
    {
        AudioInit();
    }

    public void start()
    {
        AudioStart();
    }

    public void stop()
    {
        AudioStop();
    }

    public void clear()
    {
        AudioClear();
    }

    public void getData()
    {
        Debug.Log("getData");
        _length = getLength();
        Debug.Log(_length);
        audioData = new float[_length];
        for (int i = 0; i < _length; i++)
        {
            audioData[i] = GetAudioData(i);
        }

        AudioClip clip = AudioClip.Create("web", 44100 * 2, 1, 44100, false);
        clip.SetData(audioData, 0);
        Audio.clip = clip;
        Audio.Play();
    }
	// Update is called once per frame
	void Update () {
	    
	}
}
