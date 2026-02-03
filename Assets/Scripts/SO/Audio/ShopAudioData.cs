using UnityEngine;

[CreateAssetMenu(fileName = "NewShopAudioData", menuName = "AudioData/Shop Audio")]
public class ShopAudioData : ScriptableObject
{
    [Header("클릭")]
    public AudioClip[] clickClips;      // 랜덤 클릭 소리들

    [Header("구매 성공")]
    public AudioClip[] itemBuyClips;    // 아이템 구매 완료
    public AudioClip[] trainBuyClips;   // 열차 구매 완료

    [Header("업그레이드")]
    public AudioClip[] upgradeClips;    // 업그레이드 완료

    [Header("삭제")]
    public AudioClip[] deleteClips;     // 삭제 완료

    [Header("도착")]
    public AudioClip steamClip;         // 도착 증기

    // 랜덤 클릭
    public AudioClip GetRandomClickClip()
    {
        if (clickClips == null || clickClips.Length == 0) return null;
        return clickClips[Random.Range(0, clickClips.Length)];
    }
    // 랜덤 클릭
    public AudioClip GetRandomItemClip()
    {
        if (itemBuyClips == null || itemBuyClips.Length == 0) return null;
        return itemBuyClips[Random.Range(0, itemBuyClips.Length)];
    }
    // 랜덤 클릭
    public AudioClip GetRandomTrainClip()
    {
        if (trainBuyClips == null || trainBuyClips.Length == 0) return null;
        return trainBuyClips[Random.Range(0, trainBuyClips.Length)];
    }
    // 업그레이드
    public AudioClip GetRandomUpgradeClip()
    {
        if (upgradeClips == null || upgradeClips.Length == 0) return null;
        return upgradeClips[Random.Range(0, upgradeClips.Length)];
    }
    // 삭제
    public AudioClip GetRandomDeleteClip()
    {
        if (deleteClips == null || deleteClips.Length == 0) return null;
        return deleteClips[Random.Range(0, deleteClips.Length)];
    }
}
