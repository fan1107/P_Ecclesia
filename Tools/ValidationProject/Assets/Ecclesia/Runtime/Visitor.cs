using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ecclesia
{
    public sealed class Visitor : MonoBehaviour
    {
        public Vector2Int Cell { get; private set; }
        public bool Moving => route.Count > 0;
        public bool Praying { get; private set; }
        public event Action Arrived;
        readonly Queue<Vector2Int> route = new Queue<Vector2Int>();
        SpriteRenderer body, beam;
        AudioSource footsteps;
        AudioReverbFilter reverb;
        AudioClip step;
        float stepTime;
        public void Initialize()
        {
            body=gameObject.AddComponent<SpriteRenderer>();body.sprite=PixelArt.Character();body.sortingOrder=50;
            var light=new GameObject("Prayer light");light.transform.SetParent(transform,false); light.transform.localPosition=new Vector3(0,2,0);light.transform.localScale=new Vector3(.85f,4,1);
            beam=light.AddComponent<SpriteRenderer>();beam.sprite=PixelArt.White;beam.color=new Color(1,.9f,.6f,.15f);beam.sortingOrder=49;light.SetActive(false);
            footsteps=gameObject.AddComponent<AudioSource>();footsteps.playOnAwake=false;footsteps.volume=.18f;
            reverb=gameObject.AddComponent<AudioReverbFilter>();
            var samples=new float[4410]; var random=new System.Random(42);
            for(int i=0;i<samples.Length;i++) { float t=i/44100f; samples[i]=(float)((random.NextDouble()*2-1)*.6+Math.Sin(t*2*Math.PI*130)*.4)*Mathf.Exp(-t*65); }
            step=AudioClip.Create("Soft footstep prototype",samples.Length,1,44100,false);step.SetData(samples,0);
        }
        public void Place(Vector2Int p, bool indoors)
        {
            Stop();Cell=p;transform.position=new Vector3(p.x+.5f,p.y+.5f,0);
            reverb.reverbPreset=indoors?AudioReverbPreset.StoneCorridor:AudioReverbPreset.Off;
        }
        public bool MoveTo(WalkGrid grid,Vector2Int target)
        {
            // Finish the current segment before taking a new route: never cut a corner through a pew.
            var start=route.Count>0?route.Peek():Cell;
            var path=grid.FindPath(start,target); if(path==null)return false;
            bool inSegment=route.Count>0;route.Clear();if(inSegment)route.Enqueue(start);
            foreach(var p in path)route.Enqueue(p);
            SetPrayer(false);if(route.Count==0)Arrived?.Invoke();return true;
        }
        public void Stop() { route.Clear();SetPrayer(false); if(body)body.transform.localScale=Vector3.one; }
        public void SetPrayer(bool value) { Praying=value;if(beam)beam.gameObject.SetActive(value); }
        void Update()
        {
            if(Praying)beam.color=new Color(1,.9f,.6f,.13f+.035f*Mathf.Sin(Time.time*1.4f));
            if(!Moving)return;
            var p=route.Peek();var destination=new Vector3(p.x+.5f,p.y+.5f,0);
            body.flipX=destination.x<transform.position.x;
            transform.position=Vector3.MoveTowards(transform.position,destination,3.5f*Time.deltaTime);
            transform.localScale=new Vector3(1,1+.035f*Mathf.Sin(Time.time*20),1);
            stepTime-=Time.deltaTime;if(stepTime<=0){footsteps.pitch=UnityEngine.Random.Range(.9f,1.1f);footsteps.PlayOneShot(step);stepTime=.36f;}
            if(Vector3.Distance(transform.position,destination)<.001f){Cell=route.Dequeue();if(!Moving){transform.localScale=Vector3.one;Arrived?.Invoke();}}
        }
        void OnApplicationPause(bool paused) { if(paused){Stop();transform.position=new Vector3(Cell.x+.5f,Cell.y+.5f,0);} }
        void OnDestroy(){if(step)Destroy(step);if(body&&body.sprite){Destroy(body.sprite.texture);Destroy(body.sprite);}}
    }
}
