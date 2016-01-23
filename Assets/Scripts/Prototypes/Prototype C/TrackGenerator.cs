using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TrackGenerator : MonoBehaviour {

    public GameObject[] chunkTypes;

    private List<GameObject> chunks = new List<GameObject>();
    
    void Start() {
        Vector3 startPosition = Vector3.zero;
        Quaternion startRotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
        GenerateTrack(startPosition, startRotation);
    }
    
    public void GenerateTrack(Vector3 position, Quaternion rotation) { 
        for (var i = 0; i < 10; i++)
        {
            GameObject chunk = Object.Instantiate(chunkTypes[i % chunkTypes.Length], position, rotation) as GameObject;
            Transform nextTransform = chunk.GetComponent<ChunkLink>().endTransform;
            position = nextTransform.position;
            rotation = nextTransform.rotation;
            chunks.Add(chunk);
        }
    }
}
