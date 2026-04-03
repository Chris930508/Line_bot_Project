using Azure.Core;
using Line_bot.Models;
using Microsoft.AspNetCore.Mvc;



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
            var lineEvent = receivedMessage.events[0];
            var replytoken = lineEvent.replyToken;
            var userId = lineEvent.source.userId;
            var channelAccessToken = _configuration["LineBot:ChannelAccessToken"];
            double companyLat = _configuration.GetValue<double>("CheckInSettings:CompanyLat");
            double companyLng = _configuration.GetValue<double>("CheckInSettings:CompanyLng");
            double allowDistance = _configuration.GetValue<double>("CheckInSettings:AllowDistance");
            string companyName = _configuration.GetValue<string>("CheckInSettings:CompanyName");


            if ((lineEvent.message.type == "text")&&(lineEvent.message.text.Contains("打卡座標")))
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

                    };
                    _webContext.Check.Add(newRecord);
                    _webContext.SaveChanges();
                    string successMsg = $"✅ 上班打卡成功！\n⏰ 時間：{DateTime.Now:HH:mm}";
                    isRock.LineBot.Utility.ReplyMessage(replytoken, successMsg, channelAccessToken);

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
                    var successtext = $"✅ 下班打卡成功！\n今日上班時間：{ontime:HH:mm}\n今日總工時：{time}\n辛苦了！";
                    isRock.LineBot.Utility.ReplyMessage(replytoken, successtext, channelAccessToken);

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
                     string mesg= "📌 您的最近打卡紀錄：\n";
                        
                        foreach(var r in myrecords)
                    {
                        mesg += $"{r.Checktime.Value:MM/dd HH:mm} |{r.Category}\n";
                    }
                  isRock.LineBot.Utility.ReplyMessage (replytoken, mesg.Trim(), channelAccessToken);
                
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

    }
}
