#  LINE Messaging API 與 LIFF 開發的打卡系統

> 員工點選 LINE 選單後開啟 LIFF 網頁，自動取得 GPS 座標回傳後端驗證，打卡紀錄存入 SQL Server，整個系統以 Docker 容器化部署至 Azure。

---

##  專案資源與示範影片

* 🎬 [觀看系統操作示範影片 (YouTube)](https://youtube.com/shorts/U6bdkFMjfAo?si=iXpH4v9_cTMDQvTi)
---

##  功能特色

* **LIFF 定位打卡**：開啟 LIFF 頁面自動請求 GPS 定位，提升使用者體驗。
* **GPS 範圍驗證**：後端即時計算使用者與指定地點距離，防止遠端虛假打卡。
* **精美 Flex Message**：透過動態 JSON 生成技術，回傳視覺化的打卡結果與歷史紀錄。
* **環境配置分離**：利用 IConfiguration (DI) 實現設定與代碼分離，提升系統安全性。
* **容器化與自動化 (CI/CD)**：
  * **Docker**：完成鏡像打包，實現地端與雲端環境的一致性。
  * **CI/CD**：曾實作 GitHub Actions 自動化流程，透過 Azure Webhook 實現推送到 master 分支即自動部署。

---

##  技術架構

| 類別 | 技術 |
|------|------|
| 後端框架 | ASP.NET Core 8 |
| 前端（LIFF）| HTML + JS |
| 聊天機器人 | LINE Messaging API |
| 資料庫 | Ms SQL + Entity Framework Core |
| 容器化 | Docker |
| 部署 | Azure App Service |

---

##  打卡流程

```

使用者點選 LINE 選單
      ↓
開啟 LIFF 頁面
      ↓
瀏覽器請求 GPS 定位（Geolocation API）
      ↓
LIFF 將座標 POST 至後端 API 
      ↓
後端驗證距離是否在允許範圍內
      ↓
寫入 SQL Server，LINE Bot 回傳打卡結果
