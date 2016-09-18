using UnityEngine;
using MongoDB.Driver;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using UnityEngine.UI;

public class GameOver : MonoBehaviour
{
    [BsonIgnoreExtraElements]
    public class UserScore
    {
        public ObjectId _id { get; set; }
        public string user_id { get; set; }
        public double money { get; set; }
        public double landPrice { get; set; }
        public int buyCount { get; set; }
        public int sellCount { get; set; }
        public double totalMonthlyRent { get; set; }
        public double totalPriceDifference { get; set; }
    }

    public GameObject price;
    public GameObject landPrice;
    public GameObject buyCount;
    public GameObject sellCount;
    public GameObject totalMonthlyRent;
    public GameObject totalPriceDifference;

    void Start()
    {
        string serverIp = "mongodb://125.209.192.227:27017";
        MongoClient client = new MongoClient(serverIp);
        MongoDatabase mDatabase = client.GetServer().GetDatabase("speculators");
        MongoCollection<UserScore> mUserScoreCollection = mDatabase.GetCollection<UserScore>("user_score");

        IMongoQuery query = Query<UserScore>.EQ(UserScore => UserScore.user_id, InGame.mUserId);
        UserScore userScore = mUserScoreCollection.FindOne(query);

        price.GetComponent<Text>().text = userScore.money.ToString();
        landPrice.GetComponent<Text>().text = userScore.landPrice.ToString();
        buyCount.GetComponent<Text>().text = userScore.buyCount.ToString();
        sellCount.GetComponent<Text>().text = userScore.sellCount.ToString();
        totalMonthlyRent.GetComponent<Text>().text = userScore.totalMonthlyRent.ToString();
        totalPriceDifference.GetComponent<Text>().text = userScore.totalPriceDifference.ToString();
    }

    public void OkClicked()
    {
        Debug.Log("Ok clicked");

        Application.Quit();
    }
}
