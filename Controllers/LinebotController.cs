using Azure.Core;
using isRock.LineBot;
using Line_bot.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System.Timers;

namespace Line_bot.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LinebotController : ControllerBase
    {
       
        private readonly WebContext _webContext;
        private readonly IConfiguration _configuration;
        public LinebotController(WebContext webContext , IConfiguration configuration)
        {
            _webContext = webContext;
            _configuration = configuration;
        }
       

        // GET: api/<LinebotController>
        [HttpGet]
        public IEnumerable<Check> Get()
        {
            return _webContext.Check;
        }

        // GET api/<LinebotController>/5
        [HttpGet("/find/{id}")]
        public ActionResult <int> Get(int id)
        {
            var check = _webContext.Check.Find(id);
            if (check == null)
            {
                return NotFound("找不到");           }
            return check.Id;
        }

        // POST api/<LinebotController>
        [HttpPost]
       
        public async Task<IActionResult> Post()
        {
            // 使用 StreamReader 非同步讀取
            string bodyContent = await new StreamReader(Request.Body).ReadToEndAsync();
            
            var receivedMessage = isRock.LineBot.Utility.Parsing(bodyContent);
            if (receivedMessage == null || receivedMessage.events == null || receivedMessage.events.Count == 0)
            {
                // 這是為了應付 LINE Developers 的 Verify 按鈕（它會傳送空事件）
                return Ok();
            }
            var lineEvent = receivedMessage.events[0];
            var replytoken = lineEvent.replyToken;
            var userId = lineEvent.source.userId;
            var channelAccessToken = _configuration["LineBot:ChannelAccessToken"];
            double companyLat = _configuration.GetValue<double>("CheckInSettings:CompanyLat");
            double companyLng = _configuration.GetValue<double>("CheckInSettings:CompanyLng");
            double allowDistance = _configuration.GetValue<double>("CheckInSettings:AllowDistance");
            string companyName = _configuration["CheckInSettings:CompanyName"];

            if ((lineEvent.message.type == "text") && (lineEvent.message.text.Contains("打卡座標")))
            {


                var data = lineEvent.message.text.Replace("打卡座標:", "").Split(',');


                double lat = double.Parse(data[0]);
                double lng = double.Parse(data[1]);
                string addr = lineEvent.message.address;

                double dist = GetDistance(lat, lng, companyLat, companyLng);

                if (dist <= allowDistance)
                {
                    var newRecord = new Check
                    {
                        Address = addr,
                        Lineuserid = userId,
                        Category = "上班打卡",
                        Checktime = DateTime.Now,
                        Distance = (decimal)dist,
                    };

                    _webContext.Check.Add(newRecord);
                    _webContext.SaveChanges();


                    isRock.LineBot.Utility.ReplyMessageWithJSON(replytoken, GetFlexJson("上班打卡", "#1DB446", ""), channelAccessToken);
                }
                else
                {
                    string Msg = $"❌ 打卡失敗！你距離{companyName}還有 {dist.ToString("f2")} 公里";
                    isRock.LineBot.Utility.ReplyMessage(replytoken, Msg, channelAccessToken);
                }

            }
            else if ((lineEvent.message.type == "text") && (lineEvent.message.text == "下班打卡"))
            {
                DateTime Todaystart = DateTime.Today;
                var startRecord = _webContext.Check
                     .Where(c => c.Lineuserid == userId && c.Checktime >= Todaystart && c.Category == "上班打卡")
                     .OrderBy(c => c.Checktime)
                     .FirstOrDefault();
                if (startRecord == null)
                {
                    isRock.LineBot.Utility.ReplyMessage(replytoken, "系統查無您今日的「上班打卡」紀錄，請聯繫管理員或確認是否漏打卡！", channelAccessToken);
                }
                else
                {
                    var offtime = DateTime.Now;
                    var ontime = startRecord.Checktime.Value;
                    TimeSpan duration = offtime - ontime;
                    string time = $"{duration.Hours}小時{duration.Minutes}分鐘";

                    var offWork = new Check
                    {
                        Lineuserid = userId,
                        Checktime = DateTime.Now,
                        Category = "下班打卡"
                    
                    };
                    _webContext.Check.Add(offWork);
                    _webContext.SaveChanges();

                    isRock.LineBot.Utility.ReplyMessageWithJSON(replytoken, GetFlexJson("下班打卡", "#FF5722", time), channelAccessToken);

                }

            }
            else if ((lineEvent.message.type == "text") && (lineEvent.message.text == "查詢紀錄"))
            {
                var myrecords = _webContext.Check
                      .Where(c => c.Lineuserid == userId)
                      .OrderByDescending(c => c.Checktime)
                      .Take(8)
                      .ToList();
                if (myrecords.Count == 0)
                {
                    isRock.LineBot.Utility.ReplyMessage(replytoken, "您目前尚無打卡紀錄", channelAccessToken);
                }
                else
                {
                    //   string mesg= "📌 您的最近打卡紀錄：\n";

                    //      foreach(var r in myrecords)
                    {
                        //        mesg += $"{r.Checktime.Value:MM/dd HH:mm} |{r.Category}\n";
                    }
                    // isRock.LineBot.Utility.ReplyMessage (replytoken, mesg.Trim(), channelAccessToken);

                    isRock.LineBot.Utility.ReplyMessageWithJSON(replytoken, GetHistoryFlexJson(myrecords), channelAccessToken);
                }


            }
           
                return Ok();
        }


        // PUT api/<LinebotController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<LinebotController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }

        //兩者座標距離公式
        private double GetDistance(double lat1, double lng1, double lat2, double lng2)
        {
            double r = 6371; // 地球半徑 (km)
            double dLat = (lat2 - lat1) * Math.PI / 180;
            double dLng = (lng2 - lng1) * Math.PI / 180;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                       Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return r * c; // 回傳公里數
        }

        private string GetHistoryFlexJson(System.Collections.Generic.IEnumerable<dynamic> myrecords)
        {
            var rows = new System.Collections.Generic.List<string>();

            foreach (var r in myrecords)
            {
                // 1. 強制轉換時間與類別，確保不因 dynamic 導致轉型失敗
                DateTime dt = (DateTime)r.Checktime;
               
                string category = r.Category.ToString();
                string color = category.Contains("上班") ? "#1DB446" : "#FF5722";

                
                rows.Add($@"{{
            ""type"": ""box"",
            ""layout"": ""horizontal"",
            ""margin"": ""md"",
            ""contents"": [
                {{ ""type"": ""text"", ""text"": ""{dt:MM/dd HH:mm}"", ""size"": ""sm"", ""color"": ""#666666"", ""flex"": 3 }},
                {{ ""type"": ""text"", ""text"": ""{category}"", ""size"": ""sm"", ""color"": ""{color}"", ""align"": ""end"", ""weight"": ""bold"", ""flex"": 2 }}
            ]
        }}");
            }

            string allRowsJson = string.Join(",", rows);

            return $@" [{{ ""type"": ""flex"", ""altText"": ""紀錄查詢"", ""contents"": {{
        ""type"": ""bubble"",
        ""header"": {{ ""type"": ""box"", ""layout"": ""vertical"", ""contents"": [{{ ""type"": ""text"", ""text"": ""📌 最近打卡紀錄"", ""weight"": ""bold"", ""size"": ""lg"", ""color"": ""#1DB446"" }}] }},
        ""body"": {{ ""type"": ""box"", ""layout"": ""vertical"", ""contents"": [ {allRowsJson} ] }}
    }} }}] ";
        }



        private string GetFlexJson(string title, string titlecolor,string time)
        {
            string dateStr = DateTime.Now.ToString("yyyy/MM/dd");
            string timeStr = DateTime.Now.ToString("HH:mm");
            string location = _configuration["CheckInSettings:CompanyName"];
            string workTimeSection = "";

            if (!string.IsNullOrEmpty(time))
            {
                workTimeSection = $@",
                  {{
                    ""type"": ""box"",
                    ""layout"": ""horizontal"",
                    ""contents"": [
                      {{ ""type"": ""text"", ""text"": ""今日總工時"", ""size"": ""sm"", ""color"": ""#555555"", ""flex"": 0 }},
                      {{ ""type"": ""text"", ""text"": ""{time}"", ""size"": ""sm"", ""color"": ""#111111"", ""align"": ""end"", ""weight"": ""bold"" }}
                    ]
                  }}";
            }

            return $@"
    [
      {{
        ""type"": ""flex"",
        ""altText"": ""打卡成功通知"",
        ""contents"": {{
          ""type"": ""bubble"",
          ""body"": {{
            ""type"": ""box"",
            ""layout"": ""vertical"",
            ""contents"": [
              {{ ""type"": ""text"", ""text"": ""{title}成功"", ""weight"": ""bold"", ""color"": ""{titlecolor}"", ""size"": ""sm"" }},
              {{ ""type"": ""text"", ""text"": ""{location}"", ""weight"": ""bold"", ""size"": ""xxl"", ""margin"": ""md"" }},
              {{ ""type"": ""text"", ""text"": ""{dateStr}"", ""size"": ""xs"", ""color"": ""#aaaaaa"", ""margin"": ""xs"" }},
              {{ ""type"": ""separator"", ""margin"": ""xxl"" }},
              {{ ""type"": ""box"", ""layout"": ""vertical"", ""margin"": ""xxl"", ""spacing"": ""sm"", ""contents"": [
                  {{
                    ""type"": ""box"",
                    ""layout"": ""horizontal"",
                    ""contents"": [
                      {{ ""type"": ""text"", ""text"": ""打卡時間"", ""size"": ""sm"", ""color"": ""#555555"", ""flex"": 0 }},
                      {{ ""type"": ""text"", ""text"": ""{timeStr}"", ""size"": ""sm"", ""color"": ""#111111"", ""align"": ""end"" }}
                    ]
                  }}
                  {workTimeSection} 
              ]}}
            ]
          }}
        }}
      }}
    ]";
        }


    }
}
