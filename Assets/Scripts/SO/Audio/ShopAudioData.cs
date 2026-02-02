using UnityEngine;

[CreateAssetMenu(fileName = "NewShopAudioData", menuName = "AudioData/Shop Audio")]
public class ShopAudioData : ScriptableObject
{
    [Header("클릭")]
    public AudioClip[] clickClips;      // 랜덤 클릭 소리들

    [Header("구매 성공")]
    public AudioClip itemBuySuccess;    // 아이템 구매 완료
    public AudioClip trainBuySuccess;   // 열차 구매 완료

    [Header("업그레이드")]
    public AudioClip[] upgradeClips;    // 업그레이드 완료

    [Header("도착")]
    public AudioClip steamClip;         // 도착 증기

    // 랜덤 클릭
    public AudioClip GetRandomClickClip()
    {
        if (clickClips == null || clickClips.Length == 0) return null;
        return clickClips[Random.Range(0, clickClips.Length)];
    }
    // 업그레이드
    public AudioClip GetRandomUpgradeClip()
    {
        if (upgradeClips == null || upgradeClips.Length == 0) return null;
        return upgradeClips[Random.Range(0, upgradeClips.Length)];
    }
}
