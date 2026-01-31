using Photon.Pun;
using UnityEngine;


public enum FootStepSoundType
{ 
    Walk,
    Jump,
    Land,
}

public class PlayerSoundHandler : MonoBehaviourPun
{
    // 소리 타입

    [Header("플레이어 사운드 데이터")]
    [SerializeField] PlayerAudioData _audioData;

    [Header("오디오 소스")]
    [SerializeField] AudioSource _footStepSource;   // 발소리 소스
    [SerializeField] AudioSource _weaponSource;     // 무기 소스
    [SerializeField] AudioSource _hitSource;        // 피격 소스
    [SerializeField] AudioSource _etcSource;       // 각종 기타 소스 (사망, 섭취, 픽업, 수납, 드랍)

    [Header("바닥 감지 설정")]
    [SerializeField] LayerMask _trainFloorLayer; // 열차 레이어
    [SerializeField] float _rayDistance = 1.5f;  // 바닥 재질 체크 거리



    private RaycastHit _hit;    // 발 밑 재질 체크


    // 시작 오디오 소스 설정 
    public void SetttingAudioSource(bool isLocal)
    {
        // 하나로 묶어서
        AudioSource[] sources = { _footStepSource, _weaponSource, _etcSource, _hitSource };

        // 순회
        foreach (var source in sources)
        {
            if (source == null) continue;

            if (isLocal)
            {
                // 로컬은 2d
                source.spatialBlend = 0f;
            }
            else
            {
                // 리모트는 3D
                source.spatialBlend = 1f;
                source.dopplerLevel = 0f;
            }
        }
    }



    // 걷기 (PlayerItemMoveHandler)
    public void PlayWalk() => PlaySoundAction(FootStepSoundType.Walk);

    // 점프 (PlayerMovementHandler)
    public void PlayJump() => PlaySoundAction(FootStepSoundType.Jump);

    // 착지 (PlayerMovementHandler)
    public void PlayLand() => PlaySoundAction(FootStepSoundType.Land);


    // 피격 (PlayerStatHandler)
    public void PlayHitSound()
    {
        // 피격 소리는 나만
        if (_audioData == null) return;
        PlayClip(_hitSource, _audioData.GetHitClip());
    }

    // 사망 (PlayerHandler)
    public void PlayDieSound()
    {
        if (_audioData == null) return;

        PlayClip(_etcSource, _audioData.dieClip);

        if (photonView.IsMine == true)
            photonView.RPC(nameof(RPC_DieSound), RpcTarget.Others);
    }

    // 섭취 (PlayerItemHandler)
    public void PlayEatSound(string itemName)
    {
        if (string.IsNullOrEmpty(itemName)) return;

        // 아이템 이름으로 클립 가져오기
        if (ItemManager.Instance.ItemDict.TryGetValue(itemName, out ShopItem itemData))
        {
            // 포션류 맞으면 재생
            if (itemData is PotionData data)
                PlayClip(_etcSource, data.eatClip);
        }

        if (photonView.IsMine == true)
            photonView.RPC(nameof(RPC_ItemSound), RpcTarget.Others, itemName);
    }

    // 발사 (ItemVisualHandler)
    public void PlayFireSound(WeaponData weaponData)
    {
        if (weaponData == null) return;

        // 랜덤 발사 클립 가져와서 재생
        PlayClip(_weaponSource, weaponData.GetFireClip());
    }



    // 타입에 맞는 사운드 재생
    private void PlaySoundAction(FootStepSoundType type)
    {
        // 바닥 재질 확인
        bool isTrain = IsTrainFloor();

        // 나 먼저 일단 재생
        PlaySound(type, isTrain);

        // 남들한테 전송
        if (photonView.IsMine == true)
            photonView.RPC(nameof(RPC_PlaySound), RpcTarget.Others, type, isTrain);
    }


    // 남들도 재생
    [PunRPC]
    private void RPC_PlaySound(FootStepSoundType type, bool isTrain)
    {
        // 리모트 플레이어가 보낸 정보대로 재생
        PlaySound(type, isTrain);
    }

    // 클립 골라서 재생
    private void PlaySound(FootStepSoundType type, bool isTrain)
    {
        if (_audioData == null) return;

        // 발자국 타입에 맞는 클립 가져오기
        AudioClip clip = _audioData.GetClip(type, isTrain);

        // 재생
        PlayClip(_footStepSource, clip);
    }

    // 열차 바닥인지 체크
    private bool IsTrainFloor()
    {
        // 바닥 체크 레이 기준점
        Vector3 origin = transform.position + Vector3.up * 0.5f;

        // 열차 레이어 먼저 체크
        if (Physics.Raycast(origin, Vector3.down, out _hit, _rayDistance, _trainFloorLayer))
        {
            return true;
        }

        // 나머진 그냥 다 땅
        return false;
    }

    // 단일 클립 재생
    private void PlayClip(AudioSource source, AudioClip clip)
    {
        if (source == null || clip == null) return;

        // 피치 살짝 랜덤으로
        source.pitch = Random.Range(0.95f, 1.05f);

        // 재생
        if (SoundManager.Instance != null)
        {
            // 사운드 매니저한테 쿨타임 관리 받으면서 재생. 로컬 리모트 구분도 함
            SoundManager.Instance.PlayOneShot3D(source, clip, photonView.IsMine);
        }
        else
        {
            // 매니저 없으면 그냥 재생 (이건 안 일어나면 좋겠는데)
            source.PlayOneShot(clip);
        }
    }

    [PunRPC]
    private void RPC_DieSound() => PlayClip(_etcSource, _audioData.dieClip);      // 사망

    [PunRPC]
    private void RPC_ItemSound(string itemName)
    {
        // 아이템 이름으로 클립 가져오기
        if (ItemManager.Instance.ItemDict.TryGetValue(itemName, out ShopItem itemData))
        {
            if(itemData is PotionData data)
                PlayClip(_etcSource, data.eatClip);
        }
    }   
}
