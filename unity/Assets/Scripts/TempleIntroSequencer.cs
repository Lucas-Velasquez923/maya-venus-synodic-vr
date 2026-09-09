using System.Collections;
using UnityEngine;

public class TempleIntroSequencer : MonoBehaviour
{
    public AudioSource jungleSpawnAudio;
    public GameObject templeAudioBlock;
    public AudioSource templeAudio;
    public GameObject[] templeAudioBlocksToDisable;

    void Start()
    {
        StartCoroutine(WaitForAudioThenDisable(jungleSpawnAudio, new[] { templeAudioBlock }));
        StartCoroutine(WaitForAudioThenDisable(templeAudio, templeAudioBlocksToDisable));
    }

    private IEnumerator WaitForAudioThenDisable(AudioSource audioSource, GameObject[] blocksToDisable)
    {
        yield return new WaitUntil(() => audioSource.isPlaying);
        yield return new WaitWhile(() => audioSource.isPlaying);

        foreach (var block in blocksToDisable)
        {
            if (block != null)
            {
                block.SetActive(false);
            }
        }
    }
}
