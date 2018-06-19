﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class SoundVisual : MonoBehaviour {
    private const int SAMPLE_SIZE = 1024;

    public string path;

    // Following 3 fields are updated in each frame
    // Average audio power as voltage
    public float rmsValue;
    public float dbValue;
    // Frequency of the lowest note
    public float pitchValue;

    public float backgroundIntensity;
    public Material backgroundMaterial;
    public Color minimumColor;
    public Color maximumColor;

    public float maxVisualScale = 25.0f;
    public float visualModifier = 50.0f;
    public float smoothSpeed = 10.0f;
    public float keepPercentage = 0.5f;
    
    private AudioSource source;
    private float[] samples;
    private float[] spectrum;
    private float sampleRate;

    private Transform[] visualList;
    private float[] visualScale;
    public int amnVisual = 64;

	void Start () {
        path = EditorUtility.OpenFilePanel("Select song", "", "mp3");
        ImportAudio();
        
        samples = new float[SAMPLE_SIZE];
        spectrum = new float[SAMPLE_SIZE];
        sampleRate = AudioSettings.outputSampleRate;

        //SpawnLine();
        
    }

    private IEnumerator ImportAudio()
    {
        
        string url = "file:///" + path;
        WWW audio = new WWW(url);

        while (audio.progress < 0.1)
        {
            yield return new WaitForSeconds(0.1f);
        }

        GetComponent<AudioSource>().clip = audio.GetAudioClip();
        source = GetComponent<AudioSource>();
        SpawnCircle();
    }
	private void SpawnLine()
    {
        visualScale = new float[amnVisual];
        visualList = new Transform[amnVisual];

        for (int i = 0; i < amnVisual; i++)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube) as GameObject;
            visualList[i] = go.transform;
            visualList[i].position = Vector3.right * i;
        }

    }
    private void SpawnCircle()
    {
        visualScale = new float[amnVisual];
        visualList = new Transform[amnVisual];
        Vector3 center = Vector3.zero;
        float radius = 10.0f;

        for (int i = 0; i < amnVisual; i++)
        {
            float ang = i * 1.0f / amnVisual;
            ang = ang * Mathf.PI * 2;

            float x = center.x + Mathf.Cos(ang) * radius;
            float y = center.y + Mathf.Sin(ang) * radius;

            Vector3 pos = center + new Vector3(x, y, 0);

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube) as GameObject;
            go.transform.position = pos;
            go.transform.rotation = Quaternion.LookRotation(Vector3.forward, pos);
            visualList[i] = go.transform;
        }

    }



    void Update () {
        AnalyzeSound();
        UpdateVisual();
        UpdateBackground();
    }
    private void UpdateVisual()
    {
        int visualIndex = 0;
        int spectrumIndex = 0;
        int averageSize = (int)((SAMPLE_SIZE * keepPercentage) / amnVisual);

        while (visualIndex < amnVisual)
        {
            int j = 0;
            float sum = 0;
            while (j < averageSize)
            {
                sum += spectrum[spectrumIndex];
                spectrumIndex++;
                j++;
            }
            float scaleY = sum / averageSize * visualModifier;
            visualScale[visualIndex] -= Time.deltaTime * smoothSpeed;
            if(visualScale[visualIndex] < scaleY)
            {
                visualScale[visualIndex] = scaleY;

                if(visualScale[visualIndex] > 10)
                Debug.Log(visualScale[visualIndex]);
            }

            if (visualScale[visualIndex] > maxVisualScale)
            {
                visualScale[visualIndex] = maxVisualScale;
                
            }


            visualList[visualIndex].localScale = Vector3.one + Vector3.up * visualScale[visualIndex];
            visualIndex++; ;

        }

    }
    private void AnalyzeSound()
    {
        source.GetOutputData(samples, 0);

        // Get the RMS
        // Formula: https://en.wikipedia.org/wiki/Root_mean_square#Definition
        float sum = 0;
        for (int i = 0; i < SAMPLE_SIZE; i++)
        {
            sum += samples[i] * samples[i];
        }
        rmsValue = Mathf.Sqrt(sum / SAMPLE_SIZE);

        // Get dB value
        dbValue = 20 * Mathf.Log10(rmsValue / 0.1f);

        // Get sound spectrum
        source.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);
    }
    private void UpdateBackground(){
        backgroundIntensity -= Time.deltaTime * smoothSpeed;
        int capDb = 40;
        if (backgroundIntensity < dbValue / capDb)
            backgroundIntensity = dbValue / capDb;

        backgroundMaterial.color = Color.Lerp(minimumColor, maximumColor, - backgroundIntensity);
    }
}
