using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson.Serialization.Attributes;

public class InGame : MonoBehaviour
{
    private enum MAPTYPE
    {
        ROADMAP,
        SATELLITE,
    }

    public class LandInfo
    {
        public string 시도 { get; set; }
        public string 시군구 { get; set; }
        public string 읍면동리 { get; set; }
        public string 지번 { get; set; }
        public double 가격 { get; set; }
        public double 수수료 { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class Data
    {
        public ObjectId _id { get; set; }
        //public int 일련번호 { get; set; }  // This field's value is null, since its type is undefined. Can't figure it out! :(
        public string 시도 { get; set; }
        public string 시군구 { get; set; }
        public string 읍면동리 { get; set; }
        public string 지번 { get; set; }
        public double 가격 { get; set; }
        public string 이용상황 { get; set; }
        public string 용도지역 { get; set; }
        public string 주위환경 { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class UserLandInfo
    {
        public ObjectId _id { get; set; }
        public string user_id { get; set; }
        public string year { get; set; }
        public string 시도 { get; set; }
        public string 시군구 { get; set; }
        public string 읍면동리 { get; set; }
        public string 지번 { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class UserLog
    {
        public ObjectId _id { get; set; }
        public string user_id { get; set; }
        public string time { get; set; }
        public LandInfo buy { get; set; }
        public LandInfo sell { get; set; }
        public double money_delta { get; set; }
        public double money_total { get; set; }
    }

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

    private readonly string[] YEARS = { "2005", "2006", "2007", "2008", "2009", "2011", "2012", "2013", "2014", "2015", "2016" };



    public RectTransform provinceContent;
    public GameObject provinceButton;
    public RectTransform landListContent;
    public GameObject landListButton;
    public GameObject landInfoPrice;
    public GameObject landInfoCommision;
    public GameObject landInfoUsage;
    public GameObject landInfoPurpose;
    public GameObject landInfoSurroundings;
    public RectTransform myLandListContent;
    public GameObject myLandListButton;
    public GameObject year;
    public GameObject month;
    public GameObject date;
    public GameObject hour;
    public GameObject money;

    public static readonly string mUserId = (DateTime.UtcNow - new DateTime(2016, 9, 18, 0, 0, 0)).TotalMilliseconds.ToString();
    private const string mKey = "&key=AIzaSyDxxWZ8-6t1R0WbY0yZlL9JJaizK8zLEgY";
    private const string mInitialUrl = "https://maps.googleapis.com/maps/api/staticmap?" +
        "center=대한민국&" +
        "zoom=6&" +
        "size=450x360&" +
        "maptype=hybrid";
    
    private string mCity;
    private string mProvince;
    private string mStreetAddress;
    private string mLotNumber;

    private int mZoom;
    private MAPTYPE mMapType;

    private MongoDatabase mDatabase;
    private MongoCollection<Data> mDataCollection;
    private MongoCollection<UserLandInfo> mUserLandInfoCollection;
    private MongoCollection<UserLog> mUserLogCollection;
    private MongoCollection<UserScore> mUserScoreCollection;

    private int mCurrentYear;
    private int mCurrentMonth;
    private int mCurrentDate;
    private double mCurrentHour;
    private double mMoney;
    private LandInfo mCurrentLandInfo;
    private LandInfo mCurrentMyLandInfo;
    private double mMyTotalLandPrice;

    private int mBuyCount;
    private int mSellCount;
    private double mTotalMonthlyRent;
    private double mTotalPriceDifference;



    IEnumerator Start()
    {
        Initialize();
        UpdateTime();
        UpdateMoney();
        HideDynamicButtons();
        ConnectMongoDB();

        WWW www = new WWW(mInitialUrl + mKey);
        yield return www;
        GetComponent<Renderer>().material.mainTexture = www.texture;
    }

    void Update()
    {
        mCurrentHour = mCurrentHour + 1.0 / 60.0 * 24.0 * 30.0;
        if(mCurrentHour >= 24.0)
        {
            mCurrentDate += (int)mCurrentHour / 24;
            mCurrentHour %= 24;
        }

        if(mCurrentDate > 30)
        {
            mCurrentMonth += mCurrentDate / 30;
            mCurrentDate %= 30;
        }

        if(mCurrentMonth > 12)
        {
            mCurrentYear += mCurrentMonth / 12;
            mCurrentMonth %= 12;
            
            if(mCurrentYear <= 9)
            {
                mDataCollection = mDatabase.GetCollection<Data>(YEARS[mCurrentYear]);
            }
        }

        if(mCurrentYear > 9)
        {
            double landPrice = 0.0;
            IMongoQuery query = Query<UserLandInfo>.EQ(UserLandInfo => UserLandInfo.user_id, mUserId);
            MongoCursor<UserLandInfo> cursor = mUserLandInfoCollection.Find(query);
            foreach (var myLandInfo in cursor)
            {
                query = Query.And(
                    Query<Data>.Matches(Data => Data.시도, myLandInfo.시도),
                    Query<Data>.EQ(Data => Data.시군구, myLandInfo.시군구),
                    Query<Data>.EQ(Data => Data.읍면동리, myLandInfo.읍면동리),
                    Query<Data>.EQ(Data => Data.지번, myLandInfo.지번)
                    );
                Data data = mDataCollection.FindOne(query); // The result is unique.

                landPrice += data.가격;
            }            

            UserScore userScore = new UserScore();
            userScore.user_id = mUserId;
            userScore.money = mMoney;
            userScore.landPrice = landPrice;
            userScore.buyCount = mBuyCount;
            userScore.sellCount = mSellCount;
            userScore.totalMonthlyRent = mTotalMonthlyRent;
            userScore.totalPriceDifference = mTotalPriceDifference;

            mUserScoreCollection.Insert(userScore);

            SceneManager.LoadScene(2);
        }

        mMoney += mMyTotalLandPrice * 0.0003;
        mTotalMonthlyRent += mMyTotalLandPrice * 0.0003;

        UpdateTime();
        UpdateMoney();
    }

    public void SetCity(string city)
    {
        if (city == null)
            return;

        mCity = city;
        mProvince = null;
        mStreetAddress = null;

        Debug.Log(mCity);

        DeleteLandList();
        UpdateProvinceList();

        StartCoroutine(UpdateGoogleMap());
    }

    public void SetProvince(string province)
    {
        if (province == null)
            return;

        mProvince = province;
        mStreetAddress = null;

        Debug.Log(mCity + ", " + mProvince);

        StartCoroutine(UpdateGoogleMap());
    }

    public void SetStreetAddress(string streetAddress)
    {
        if (streetAddress == null)
            return;

        char[] delimiter = { '+' };
        string[] splitAddress = streetAddress.Split(delimiter);
        string splitstreetAddress = splitAddress[0];
        string lotNumber = splitAddress[1];

        mStreetAddress = splitstreetAddress;
        mLotNumber = lotNumber;

        Debug.Log(mCity + ", " + mProvince + ", " + mStreetAddress + ", " + mLotNumber);

        StartCoroutine(UpdateGoogleMap());
    }

    public void SetLandInfo(string landInfo)
    {
        char[] delimiter = { '/' };
        string[] splitLandInfo = landInfo.Split(delimiter);
        string city = splitLandInfo[0];
        string province = splitLandInfo[1];
        string streetAddress = splitLandInfo[2];
        string lotNumber = splitLandInfo[3];

        IMongoQuery query = Query.And(
            Query<Data>.Matches(Data => Data.시도, city),
            Query<Data>.EQ(Data => Data.시군구, province),
            Query<Data>.EQ(Data => Data.읍면동리, streetAddress),
            Query<Data>.EQ(Data => Data.지번, lotNumber)
            );
        Data data = mDataCollection.FindOne(query); // The result is unique.
        if(data == null)
        {
            return;
        }

        mCurrentLandInfo.시도 = data.시도;
        mCurrentLandInfo.시군구 = data.시군구;
        mCurrentLandInfo.읍면동리 = data.읍면동리;
        mCurrentLandInfo.지번 = data.지번;
        mCurrentLandInfo.가격 = data.가격;
        mCurrentLandInfo.수수료 = Math.Round((data.가격 * 0.006f), 2);

        landInfoPrice.GetComponent<Text>().text = mCurrentLandInfo.가격.ToString();
        landInfoCommision.GetComponent<Text>().text = mCurrentLandInfo.수수료.ToString();
        landInfoUsage.GetComponent<Text>().text = data.이용상황.ToString();
        landInfoPurpose.GetComponent<Text>().text = data.용도지역.ToString();
        landInfoSurroundings.GetComponent<Text>().text = data.주위환경.ToString();
    }

    public void SetMyLandInfo(string myLandInfo)
    {
        char[] delimiter = { '/' };
        string[] splitLandInfo = myLandInfo.Split(delimiter);
        string city = splitLandInfo[0];
        string province = splitLandInfo[1];
        string streetAddress = splitLandInfo[2];
        string lotNumber = splitLandInfo[3];

        IMongoQuery query = Query.And(
            Query<UserLandInfo>.EQ(UserLandInfo => UserLandInfo.user_id, mUserId),
            Query<UserLandInfo>.Matches(UserLandInfo => UserLandInfo.시도, city),
            Query<UserLandInfo>.EQ(UserLandInfo => UserLandInfo.시군구, province),
            Query<UserLandInfo>.EQ(UserLandInfo => UserLandInfo.읍면동리, streetAddress),
            Query<UserLandInfo>.EQ(UserLandInfo => UserLandInfo.지번, lotNumber)
            );
        UserLandInfo userLandInfo = mUserLandInfoCollection.FindOne(query); // The result is unique.
        if(userLandInfo == null)
        {
            Debug.Log("null: " + city + ", " + province + ", " + streetAddress + ", " + lotNumber);
        }

        query = Query.And(
            Query<Data>.Matches(Data => Data.시도, userLandInfo.시도),
            Query<Data>.EQ(Data => Data.시군구, userLandInfo.시군구),
            Query<Data>.EQ(Data => Data.읍면동리, userLandInfo.읍면동리),
            Query<Data>.EQ(Data => Data.지번, userLandInfo.지번)
            );
        Data data = mDatabase.GetCollection<Data>(userLandInfo.year).FindOne(query);
        if(data == null)
        {
            return;
        }

        mCurrentMyLandInfo.시도 = userLandInfo.시도;
        mCurrentMyLandInfo.시군구 = userLandInfo.시군구;
        mCurrentMyLandInfo.읍면동리 = userLandInfo.읍면동리;
        mCurrentMyLandInfo.지번 = userLandInfo.지번;
        mCurrentMyLandInfo.가격 = data.가격;
        mCurrentMyLandInfo.수수료 = Math.Round((data.가격 * 0.006f), 2);

        landInfoPrice.GetComponent<Text>().text = mCurrentMyLandInfo.가격.ToString();
        landInfoCommision.GetComponent<Text>().text = mCurrentMyLandInfo.수수료.ToString();
        landInfoUsage.GetComponent<Text>().text = data.이용상황.ToString();
        landInfoPurpose.GetComponent<Text>().text = data.용도지역.ToString();
        landInfoSurroundings.GetComponent<Text>().text = data.주위환경.ToString();
    }

    public void Search()
    {
        UpdateLandList();
        StartCoroutine(UpdateGoogleMap());
    }

    public void ZoomIn()
    {
        ++mZoom;
        StartCoroutine(UpdateGoogleMap());
    }

    public void ZoomOut()
    {
        --mZoom;
        StartCoroutine(UpdateGoogleMap());
    }

    public void Roadmap()
    {
        mMapType = MAPTYPE.ROADMAP;
        StartCoroutine(UpdateGoogleMap());
    }

    public void Satellite()
    {
        mMapType = MAPTYPE.SATELLITE;
        StartCoroutine(UpdateGoogleMap());
    }

    public void Buy()
    {
        if(mMoney < mCurrentLandInfo.가격 + mCurrentLandInfo.수수료)
        {
            Debug.Log("(Buy) Need more money.");
            return;
        }

        if(mCurrentLandInfo.시도 == null ||
           mCurrentLandInfo.시군구 == null ||
           mCurrentLandInfo.읍면동리 == null ||
           mCurrentLandInfo.지번 == null)
        {
            return;
        }

        IMongoQuery query = Query.And(
            Query<UserLandInfo>.EQ(UserLandInfo => UserLandInfo.user_id, mUserId),
            Query<UserLandInfo>.Matches(UserLandInfo => UserLandInfo.시도, mCurrentLandInfo.시도),
            Query<UserLandInfo>.EQ(UserLandInfo => UserLandInfo.시군구, mCurrentLandInfo.시군구),
            Query<UserLandInfo>.EQ(UserLandInfo => UserLandInfo.읍면동리, mCurrentLandInfo.읍면동리),
            Query<UserLandInfo>.EQ(UserLandInfo => UserLandInfo.지번, mCurrentLandInfo.지번)
            );
        UserLandInfo cursor = mUserLandInfoCollection.FindOne(query);
        if(cursor != null)
        {
            Debug.Log("(Buy) Already bought the land.");
            return;
        }

        UserLog userLog = new UserLog();
        userLog.user_id = mUserId;
        userLog.time = YEARS[mCurrentYear] + "-" + mCurrentMonth + "-" + mCurrentDate + "-" + mCurrentHour;
        userLog.buy = new LandInfo();
        userLog.buy.시도 = mCurrentLandInfo.시도;
        userLog.buy.시군구 = mCurrentLandInfo.시군구;
        userLog.buy.읍면동리 = mCurrentLandInfo.읍면동리;
        userLog.buy.지번 = mCurrentLandInfo.지번;
        userLog.buy.가격 = mCurrentLandInfo.가격;
        userLog.buy.수수료 = mCurrentLandInfo.수수료;
        userLog.sell = null;
        userLog.money_delta = -1.0 * (mCurrentLandInfo.가격 + mCurrentLandInfo.수수료);
        userLog.money_total = mMoney - mCurrentLandInfo.가격 - mCurrentLandInfo.수수료;

        WriteConcernResult result = mUserLogCollection.Insert(userLog);
        if (result.Ok == false)
        {
            Debug.Log("(Buy) Database user log insertion failed.");
            return;
        }

        UserLandInfo userLandInfo = new UserLandInfo();
        userLandInfo.user_id = mUserId;
        userLandInfo.year = YEARS[mCurrentYear].ToString();
        userLandInfo.시도 = mCurrentLandInfo.시도;
        userLandInfo.시군구 = mCurrentLandInfo.시군구;
        userLandInfo.읍면동리 = mCurrentLandInfo.읍면동리;
        userLandInfo.지번 = mCurrentLandInfo.지번;

        result = mUserLandInfoCollection.Insert(userLandInfo);
        if (result.Ok == false)
        {
            Debug.Log("(Buy) Database user info insertion failed.");
            return;
        }

        mMoney -= (mCurrentLandInfo.가격 + mCurrentLandInfo.수수료);
        ++mBuyCount;

        UpdateMyLandList();
    }

    public void Sell()
    {
        if (mCurrentMyLandInfo.시도 == null ||
            mCurrentMyLandInfo.시군구 == null ||
            mCurrentMyLandInfo.읍면동리 == null ||
            mCurrentMyLandInfo.지번 == null)
        {
            return;
        }

        UserLog userLog = new UserLog();
        userLog.user_id = mUserId;
        userLog.time = YEARS[mCurrentYear] + "-" + mCurrentMonth + "-" + mCurrentDate + "-" + mCurrentHour;
        userLog.buy = null;
        userLog.sell = new LandInfo();
        userLog.sell.시도 = mCurrentMyLandInfo.시도;
        userLog.sell.시군구 = mCurrentMyLandInfo.시군구;
        userLog.sell.읍면동리 = mCurrentMyLandInfo.읍면동리;
        userLog.sell.지번 = mCurrentMyLandInfo.지번;
        userLog.sell.가격 = mCurrentMyLandInfo.가격;
        userLog.sell.수수료 = mCurrentMyLandInfo.수수료;
        userLog.money_delta = mCurrentMyLandInfo.가격 - mCurrentMyLandInfo.수수료;
        userLog.money_total = mMoney + mCurrentMyLandInfo.가격 - mCurrentMyLandInfo.수수료;

        WriteConcernResult result = mUserLogCollection.Insert(userLog);
        if (result.Ok == false)
        {
            Debug.Log("(Sell) Database user log insertion failed.");
            return;
        }

        IMongoQuery query = Query.And(
            Query<UserLandInfo>.EQ(UserLandInfo => UserLandInfo.user_id, mUserId),
            Query<UserLandInfo>.Matches(UserLandInfo => UserLandInfo.시도, mCurrentMyLandInfo.시도),
            Query<UserLandInfo>.EQ(UserLandInfo => UserLandInfo.시군구, mCurrentMyLandInfo.시군구),
            Query<UserLandInfo>.EQ(UserLandInfo => UserLandInfo.읍면동리, mCurrentMyLandInfo.읍면동리),
            Query<UserLandInfo>.EQ(UserLandInfo => UserLandInfo.지번, mCurrentMyLandInfo.지번)
            );
        UserLandInfo userLandInfo = mUserLandInfoCollection.FindOne(query);
        string boughtYear = userLandInfo.year;

        result = mUserLandInfoCollection.Remove(query);
        if (result.Ok == false)
        {
            Debug.Log("(Sell) Database user info deletion failed.");
            return;
        }

        mMoney += (mCurrentMyLandInfo.가격 - mCurrentMyLandInfo.수수료);
        ++mSellCount;

        double boughtPrice;
        query = Query.And(
            Query<Data>.Matches(Data => Data.시도, mCurrentMyLandInfo.시도),
            Query<Data>.EQ(Data => Data.시군구, mCurrentMyLandInfo.시군구),
            Query<Data>.EQ(Data => Data.읍면동리, mCurrentMyLandInfo.읍면동리),
            Query<Data>.EQ(Data => Data.지번, mCurrentMyLandInfo.지번)
            );
        Data data = mDatabase.GetCollection<Data>(boughtYear).FindOne(query);
        if (data == null)
        {
            boughtPrice = 0.0;
        }
        else
        {
           boughtPrice =  data.가격;
        }
        double sellPrice = mCurrentMyLandInfo.가격;

        mTotalPriceDifference += (sellPrice - boughtPrice);

        UpdateMyLandList();
    }



    private void Initialize()
    {
        mZoom = 0;
        mMapType = MAPTYPE.SATELLITE;
        mCurrentYear = 0;
        mCurrentMonth = 1;
        mCurrentDate = 1;
        mCurrentHour = 0.0;
        mMoney = 500000;
        mCurrentLandInfo = new LandInfo();
        mCurrentLandInfo.시도 = null;
        mCurrentLandInfo.시군구 = null;
        mCurrentLandInfo.읍면동리 = null;
        mCurrentLandInfo.지번 = null;
        mCurrentLandInfo.가격 = 0.0;
        mCurrentLandInfo.수수료 = 0.0;
        mCurrentMyLandInfo = new LandInfo();
        mCurrentMyLandInfo.시도 = null;
        mCurrentMyLandInfo.시군구 = null;
        mCurrentMyLandInfo.읍면동리 = null;
        mCurrentMyLandInfo.지번 = null;
        mCurrentMyLandInfo.가격 = 0.0;
        mCurrentMyLandInfo.수수료 = 0.0;
        mMyTotalLandPrice = 0.0;
        mBuyCount = 0;
        mSellCount = 0;
        mTotalMonthlyRent = 0.0;
        mTotalPriceDifference = 0.0;
}

    private void HideDynamicButtons()
    {
        provinceButton.SetActive(false);
        landListButton.SetActive(false);
        myLandListButton.SetActive(false);
    }

    private void ConnectMongoDB()
    {
        // Connect.
        string serverIp = "mongodb://125.209.192.227:27017"; // The server ip and port number.
        MongoClient client = new MongoClient(serverIp);

        // Get database.
        mDatabase = client.GetServer().GetDatabase("speculators"); // speculators is a database.

        // Get collection.
        mDataCollection = mDatabase.GetCollection<Data>(YEARS[mCurrentYear]);
        mUserLandInfoCollection = mDatabase.GetCollection<UserLandInfo>("user_land_info");
        mUserLogCollection = mDatabase.GetCollection<UserLog>("user_log");
        mUserScoreCollection = mDatabase.GetCollection<UserScore>("user_score");
    }

    private void UpdateTime()
    {
        year.GetComponent<Text>().text = YEARS[mCurrentYear];
        month.GetComponent<Text>().text = mCurrentMonth.ToString();
        date.GetComponent<Text>().text = mCurrentDate.ToString();
        hour.GetComponent<Text>().text = mCurrentHour.ToString();
    }

    private void UpdateMoney()
    {
        money.GetComponent<Text>().text = mMoney.ToString();
    }

    private void DeleteProvinceList()
    {
        for (int i = provinceContent.childCount - 1; i >= 1; --i)
        {
            DestroyImmediate(provinceContent.GetChild(i).gameObject);
        }
    }

    private void UpdateProvinceList()
    {
        // Find distinct "시군구" values where "시도" is mCity.
        IMongoQuery query = Query<Data>.Matches(Data => Data.시도, mCity);
        IEnumerable cursor = mDataCollection.Distinct("시군구", query);

        DeleteProvinceList();

        int count = 0;
        Vector3 provinceButtonTemplatePosition = provinceButton.transform.position;
        foreach (var data in cursor)
        {
            ++count;
            
            GameObject provinceButtonClone = (GameObject)Instantiate(provinceButton);
            provinceButtonClone.transform.SetParent(provinceContent, false);
            provinceButtonClone.transform.position = provinceButtonTemplatePosition + new Vector3(0, -34.3f * (float)(count - 1), 0);
            provinceButtonClone.transform.localScale = new Vector3(1, 1, 1);            
            provinceButtonClone.GetComponentInChildren<Text>().text = data.ToString();

            string argumentString = data.ToString();
            provinceButtonClone.GetComponent<Button>().onClick.AddListener(() => SetProvince(argumentString));

            provinceButtonClone.SetActive(true);
        }
        provinceContent.sizeDelta = new Vector2(0, 27.0f * (float)count + 3.0f);
    }

    private void DeleteLandList()
    {
        for (int i = landListContent.childCount - 1; i >= 1; --i)
        {
            DestroyImmediate(landListContent.GetChild(i).gameObject);
        }
    }

    private void UpdateLandList()
    {
        // Find records where "시도" is mCity AND "시군구" is mProvince.
        IMongoQuery query = Query.And(
            Query<Data>.Matches(Data => Data.시도, mCity),
            Query<Data>.EQ(Data => Data.시군구, mProvince)
            );
        MongoCursor<Data> cursor = mDataCollection.Find(query);

        DeleteLandList();

        int count = 0;
        Vector3 landListButtonTemplatePosition = landListButton.transform.position;
        foreach (var data in cursor)
        {
            ++count;
            
            GameObject landListButtonClone = (GameObject)Instantiate(landListButton);
            landListButtonClone.transform.SetParent(landListContent, false);
            landListButtonClone.transform.position = landListButtonTemplatePosition + new Vector3(0, -34.3f * (float)(count - 1), 0);
            landListButtonClone.transform.localScale = new Vector3(1, 1, 1);

            string textString = data.읍면동리 + " " + data.지번;
            landListButtonClone.GetComponentInChildren<Text>().text = textString;

            string streetAddressString = data.읍면동리 + "+" + data.지번;
            landListButtonClone.GetComponent<Button>().onClick.AddListener(() => SetStreetAddress(streetAddressString));

            string landInfoString = mCity + "/" + mProvince + "/" + data.읍면동리 + "/" + data.지번;
            landListButtonClone.GetComponent<Button>().onClick.AddListener(() => SetLandInfo(landInfoString));

            landListButtonClone.SetActive(true);
        }
        landListContent.sizeDelta = new Vector2(0, 26.8f * (float)count + 2.5f);
    }

    private void DeleteMyLandList()
    {
        for (int i = myLandListContent.childCount - 1; i >= 1; --i)
        {
            DestroyImmediate(myLandListContent.GetChild(i).gameObject);
        }
    }

    private void UpdateMyLandList()
    {
        IMongoQuery query = Query<UserLandInfo>.EQ(UserLandInfo => UserLandInfo.user_id, mUserId);
        MongoCursor<UserLandInfo> cursor = mUserLandInfoCollection.Find(query);

        DeleteMyLandList();

        int count = 0;
        Vector3 myLandListButtonTemplatePosition = myLandListButton.transform.position;
        mMyTotalLandPrice = 0.0;
        foreach (var myLandInfo in cursor)
        {
            ++count;

            query = Query.And(
                Query<Data>.Matches(Data => Data.시도, myLandInfo.시도),
                Query<Data>.EQ(Data => Data.시군구, myLandInfo.시군구),
                Query<Data>.EQ(Data => Data.읍면동리, myLandInfo.읍면동리),
                Query<Data>.EQ(Data => Data.지번, myLandInfo.지번)
                );
            Data data = mDatabase.GetCollection<Data>(myLandInfo.year).FindOne(query);

            mMyTotalLandPrice += data.가격;

            GameObject myLandListButtonClone = (GameObject)Instantiate(myLandListButton);
            myLandListButtonClone.transform.SetParent(myLandListContent, false);
            myLandListButtonClone.transform.position = myLandListButtonTemplatePosition + new Vector3(0, -34.3f * (float)(count - 1), 0);
            myLandListButtonClone.transform.localScale = new Vector3(1, 1, 1);

            string textString = myLandInfo.시도 + " " + myLandInfo.시군구 + " " + myLandInfo.읍면동리 + " " + myLandInfo.지번;
            myLandListButtonClone.GetComponentInChildren<Text>().text = textString;
            
            string myLandInfoString = myLandInfo.시도 + "/" + myLandInfo.시군구 + "/" + myLandInfo.읍면동리 + "/" + myLandInfo.지번;
            myLandListButtonClone.GetComponent<Button>().onClick.AddListener(() => SetMyLandInfo(myLandInfoString));

            myLandListButtonClone.SetActive(true);
        }
        myLandListContent.sizeDelta = new Vector2(0, 26.8f * (float)count + 2.5f);
    }

    IEnumerator UpdateGoogleMap()
    {
        if (mCity == null)
        {
            Debug.Log("(UpdateGoogleMap) mCity is null.");
            yield break;
        }

        string requestUrl = "https://maps.googleapis.com/maps/api/staticmap?";
        string option = "size=1280x768&maptype=";
        if (mMapType == MAPTYPE.ROADMAP)
        {
            option = option + "roadmap";
        }
        else if(mMapType == MAPTYPE.SATELLITE)
        {
            option = option + "hybrid";
        }
        else
        {
            Debug.Log("(UpdateGoogleMap) The argument mMapType is invalid.");
            yield break;
        }
        option = option + "&markers=color:red%7Clabel:%7C";
        
        int zoomLevel;
        string requestAddress = mCity;
        if (mProvince == null)
        {
            zoomLevel = 11;
        }
        else if(mStreetAddress == null)
        {
            zoomLevel = 14;
            requestAddress = requestAddress + "+" + mProvince;
        }
        else
        {
            zoomLevel = 17;
            requestAddress = requestAddress + "+" + mProvince + "+" + mStreetAddress + "+" + mLotNumber;
        }

        // zoomLevel must be in [0, 21].
        zoomLevel = zoomLevel + mZoom;
        if(zoomLevel < 0)
        {
            zoomLevel = 0;
        }
        else if(zoomLevel > 21)
        {
            zoomLevel = 21;
        }

        requestUrl = requestUrl + "center=" + requestAddress + "&zoom=" + zoomLevel + "&" + option + requestAddress;
        requestUrl = requestUrl.Replace(" ", "+");

        Debug.Log(requestUrl);

        WWW www = new WWW(requestUrl + mKey);
        yield return www;
        GetComponent<Renderer>().material.mainTexture = www.texture;
    }
}
