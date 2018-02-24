﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Threading;
using Intel.RealSense;
using System.Linq;

public class PointCloudGeneratorOriginal : MonoBehaviour {

    private int streamWidth;
    private int streamHeight;
    private int totalImageSize;
    private int particleSize;
    private int particleCount;
    public RealsenseDeviceOriginal realsenseDeviceOriginal;
    private ParticleSystem.Particle[] particles;
    private PointCloud pc = new PointCloud();
    //public UnityEngine.Gradient gradient;
    public float pointsSize = 0.01f;
    public int skipParticles = 2;
    public ParticleSystem pointCloudParticles;

    byte[] byteColorData;
    DepthFrame depthFrame;
    VideoFrame vidFrame;
    Points.Vertex[] vertices;
    Align aligner;

    void Start()
    {
        Application.runInBackground = true;
        particleCount = 0;

        if (realsenseDeviceOriginal.Instance.ActiveProfile != null)
        {
            OnStartStreaming(realsenseDeviceOriginal.Instance.ActiveProfile);
        }
        else
        {
            realsenseDeviceOriginal.Instance.OnStart += OnStartStreaming;
        }
        particles = new ParticleSystem.Particle[particleSize];

        aligner = new Align(Stream.Color);
    }

    private void OnStartStreaming(PipelineProfile activeProfile)
    {
        if (InitializeStream(activeProfile))
        {
            //realsenseDeviceOriginal.Instance.onNewSample += OnFrame;
            realsenseDeviceOriginal.Instance.onNewSampleSet += OnFrame;
        }
    }

    private void OnFrame(FrameSet frameset)
    {
        using (FrameSet aligned = aligner.Process(frameset))
        {
            //Depth
            depthFrame = aligned.Where(f => f.Profile.Stream == Stream.Depth).First() as DepthFrame;

            //Color
            vidFrame = aligned.Where(f => f.Profile.Stream == Stream.Color).First() as VideoFrame;


            if (depthFrame == null || vidFrame == null)
            {
                // Debug.Log("Frame is not a depth frame");
                return;
            }

           UpdateParticleParams(depthFrame.Width, depthFrame.Height, depthFrame.Profile.Format);

            var points = pc.Calculate(depthFrame);

            //Depth
            vertices = vertices ?? new Points.Vertex[points.Count];
            points.CopyTo(vertices);

            //Color
            byteColorData = byteColorData ?? new byte[vidFrame.Stride * vidFrame.Height];
            vidFrame.CopyTo(byteColorData);

            for (int index = 0; index < particleSize; index += skipParticles)
            {
                var v = vertices[index];
                
                if (v.z > 0)
                {
                    particles[index].position = new Vector3(v.x, v.y, v.z);
                    particles[index].startSize = pointsSize;
                    //particles[index].startColor = gradient.Evaluate(v.z);
                    particles[index].startColor = new Color32(byteColorData[index * 3], byteColorData[index * 3 + 1], byteColorData[index * 3 + 2], 255);
                }
                /*
                else
                {
                    particles[index].position = new Vector3(0, 0, 0);
                    particles[index].startSize = (float)0.0;
                    particles[index].startColor = new Color32(0, 0, 0, 0);
                }
                */
            }
        }
    }
    /*
    private void OnFrame(Frame frame)
    {
        if (frame.Profile.Stream == Intel.RealSense.Stream.Depth)
        {
            var depthFrame = frame as DepthFrame;
            if (depthFrame == null)
            {
                Debug.Log("Frame is not a depth frame");
                return;
            }

            UpdateParticleParams(depthFrame.Width, depthFrame.Height, depthFrame.Profile.Format);

            var points = pc.Calculate(frame);

            Points.Vertex[] vertices = new Points.Vertex[points.Count];
            points.CopyTo(vertices);
            for (int index = 0; index < vertices.Length; index += skipParticles)
            {
                var v = vertices[index];
                if (v.z > 0)
                {
                    particles[index].position = new Vector3(v.x, v.y, v.z);
                    particles[index].startSize = pointsSize;
                    particles[index].startColor = gradient.Evaluate(v.z);
                }
                else
                {
                    particles[index].position = new Vector3(0, 0, 0);
                    particles[index].startSize = (float)0.0;
                    particles[index].startColor = new Color32(0, 0, 0, 0);
                }
            }
        }
        else if (frame.Profile.Stream == Intel.RealSense.Stream.Color)
        {
            //pc.MapTexture(frame);
        }
    }*/

    private void UpdateParticleParams(int width, int height, Format format)
    {
        streamWidth = width;
        streamHeight = height;

        if (format != Format.Z16)
        {
            Debug.Log("Unsupported format");
            return;
        }

        const int bpp = 2;

        if (totalImageSize != streamWidth * streamHeight * bpp)
        {
            totalImageSize = streamWidth * streamHeight * bpp;
            particleSize = totalImageSize / skipParticles;
            particles = new ParticleSystem.Particle[particleSize];
        }

        if (particleSize != totalImageSize / skipParticles)
        {
            particleSize = totalImageSize / skipParticles;
            particles = new ParticleSystem.Particle[particleSize];
        }
        particleCount = particleSize;
    }

    void Update()
    {
        //TODO: Lock & copy particles?
        pointCloudParticles.SetParticles(particles, particleCount);
    }

    private bool InitializeStream(PipelineProfile activeProfile)
    {
        var depthStream = activeProfile.Streams.FirstOrDefault(s => s.Stream == Stream.Depth);
        if (depthStream == null)
        {
            Debug.Log("No Depth stream available");
            return false;
        }
        var depthProfile = depthStream as VideoStreamProfile;
        //depthIntrinsic = depthProfile.GetIntrinsics();
        streamWidth = depthProfile.Width;
        streamHeight = depthProfile.Height;
        return true;
    }
    /*
    private float Remap(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }*/
}
