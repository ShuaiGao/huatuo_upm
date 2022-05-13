using UnityEngine;
using System.Collections;

/*
 * copy from https://github.com/keijiro/RippleEffect
 */
public class RippleEffect
{
    private static readonly AnimationCurve waveform = new AnimationCurve(
        new Keyframe(0.00f, 0.50f, 0, 0),
        new Keyframe(0.05f, 1.00f, 0, 0),
        new Keyframe(0.15f, 0.10f, 0, 0),
        new Keyframe(0.25f, 0.80f, 0, 0),
        new Keyframe(0.35f, 0.30f, 0, 0),
        new Keyframe(0.45f, 0.60f, 0, 0),
        new Keyframe(0.55f, 0.40f, 0, 0),
        new Keyframe(0.65f, 0.55f, 0, 0),
        new Keyframe(0.75f, 0.46f, 0, 0),
        new Keyframe(0.85f, 0.52f, 0, 0),
        new Keyframe(0.99f, 0.50f, 0, 0)
    );
    
    internal class Droplet
    {
        Vector2 position;
        float time;

        public Droplet()
        {
            time = 1000;
        }

        public void Reset()
        {
            position = new Vector2(Random.value, Random.value);
            time = 0;
        }

        public void Update()
        {
            time += Time.deltaTime;
        }

        public Vector4 MakeShaderParameter(float aspect)
        {
            return new Vector4(position.x * aspect, position.y, time, 0);
        }
    }

    private float refractionStrength = 0.5f;
    private Color reflectionColor = Color.gray;
    private float reflectionStrength = 0.4f;
    private float waveSpeed = 1.25f;
    private float dropInterval = 2.5f;
    private float timer = 0;
    private  int dropCount = 0;
    private float aspect = 0;

    private Droplet[] droplets;
    private Texture2D gradTexture;
    private Material material;


    void UpdateShaderParameters()
    {
        material.SetVector("_Drop1", droplets[0].MakeShaderParameter(aspect));
        material.SetVector("_Drop2", droplets[1].MakeShaderParameter(aspect));
        material.SetVector("_Drop3", droplets[2].MakeShaderParameter(aspect));
    }

    public void Init(Material _material, float _aspect)
    {
        aspect = _aspect;
        droplets = new Droplet[3];
        droplets[0] = new Droplet();
        droplets[1] = new Droplet();
        droplets[2] = new Droplet();

        gradTexture = new Texture2D(2048, 1, TextureFormat.Alpha8, false);
        gradTexture.wrapMode = TextureWrapMode.Clamp;
        gradTexture.filterMode = FilterMode.Bilinear;
        for (var i = 0; i < gradTexture.width; i++)
        {
            var x = 1.0f / gradTexture.width * i;
            var a = waveform.Evaluate(x);
            gradTexture.SetPixel(i, 0, new Color(a, a, a, a));
        }
        gradTexture.Apply();

        material = _material;
        material.hideFlags = HideFlags.DontSave;
        material.SetTexture("_GradTex", gradTexture);
        material.SetColor("_Reflection", reflectionColor);
        material.SetVector("_Params1", new Vector4(aspect, 1, 1 / waveSpeed, 0));
        material.SetVector("_Params2", new Vector4(1, 1 / aspect, refractionStrength, reflectionStrength));
        
        UpdateShaderParameters();
    }

    public void Destroy()
    {
        GameObject.DestroyImmediate(gradTexture);
    }
    
    public void Update()
    {
        if (dropInterval > 0)
        {
            timer += Time.deltaTime;
            while (timer > dropInterval)
            {
                droplets[dropCount++ % droplets.Length].Reset();
                timer -= dropInterval;
            }
        }

        foreach (var d in droplets)
        {
            d.Update();
        }

        UpdateShaderParameters();
    }
}
