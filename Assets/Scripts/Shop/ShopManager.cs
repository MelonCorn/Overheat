using UnityEngine;

public class ShopManager : MonoBehaviour
{

    // 테스트용 열차 버튼
    public void BuyTrain(TrainType type)
    {
        // 타입에 맞는 정보 불러오기
        TrainData trainData = TrainManager.Instance.TrainDict[type];

        // 구매비용
        int price = trainData.price;

        // 골드 충분하면 골드 소모 후
        if (GameManager.Instance.TryUseGold(price))
        {
            // 열차에 추가 요청
            TrainManager.Instance.RequestAddTrain(type);
        }
    }
}
