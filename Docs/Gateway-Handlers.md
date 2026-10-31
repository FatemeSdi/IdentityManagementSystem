# هندلرهای Gateway سامانه احراز اصالت سند

به‌جای انتشار +۴۰ آدرس جداگانه، فقط **۳ آدرس** روی سرور API باید منتشر (هاست) شوند:

| هندلر | آدرس | سرویس‌های پشت آن |
|---|---|---|
| auth-handler | `https://{host}:5066/api/auth-handler` | `Auth/*` (ورود، توکن، فراموشی رمز، مدیریت کاربران و دسترسی) |
| ehraz-handler | `https://{host}:5066/api/ehraz-handler` | `Request/*`، `Cartables/*`، `Company`، `Reports/*`، `EvalAppointment/*` |
| service-handler | `https://{host}:5066/api/service-handler` | `Service/*` (شاهکار، پیامک، متن سند، قبض انبار، ...) |

> هر سه آدرس **فقط متد `POST`** با یک **body ثابت JSON** دارند؛ هیچ پارامتر query و هیچ هدر سفارشی لازم نیست.
> Gateway سازمان فقط باید body را دست‌نخورده عبور دهد و هدرهای `Authorization`، `Content-Type`، `X-API-Key` و `X-Forwarded-For` را حذف نکند.

## نحوه‌ی فراخوانی

همه‌ی سرویس‌ها (چه قبلاً GET بوده‌اند، چه PUT یا DELETE) با **POST** به هندلر فرستاده می‌شوند و همه‌ی مشخصات درخواست اصلی داخل body می‌آید:

```json
{
  "action": "Request/Track",
  "method": "GET",
  "query":  { "trackingCode": "X", "mobileNumber": "Y" },
  "body":   null
}
```

| فیلد | الزامی | توضیح |
|---|---|---|
| `action` | * | مسیر سرویس مقصد، همان مسیر قبلی بعد از `/api/` (مثلاً `Auth/UpdateUser/12`) |
| `method` | | متد اصلی سرویس (`GET`/`POST`/`PUT`/`DELETE`)؛ اگر نیاید API از تعریف سرویس پیدا می‌کند |
| `query` | | پارامترهای query سرویس؛ مقدار هر کلید رشته، عدد، بولین یا آرایه |
| `body` | | بدنه‌ی JSON خود سرویس (همان مدل قبلی) |

| قبل | body ارسالی به هندلر |
|---|---|
| `POST /api/Auth/login` | `POST /api/auth-handler` ← `{"action":"Auth/login","method":"POST","body":{"username":"...","password":"..."}}` |
| `PUT /api/Auth/UpdateUser/12` | `POST /api/auth-handler` ← `{"action":"Auth/UpdateUser/12","method":"PUT","body":{...}}` |
| `GET /api/Request/Track?trackingCode=X&mobileNumber=Y` | `POST /api/ehraz-handler` ← `{"action":"Request/Track","method":"GET","query":{"trackingCode":"X","mobileNumber":"Y"}}` |
| `GET /api/Service/GetTextByRequestId/101` | `POST /api/service-handler` ← `{"action":"Service/GetTextByRequestId/101","method":"GET"}` |

```bash
curl -X POST "https://{host}:5066/api/auth-handler" -H "Content-Type: application/json" -H "X-API-Key: ..." -d '{"action":"Auth/login","method":"POST","body":{"username":"...","password":"..."}}'
```

API قبل از اجرا، درخواست را به همان `{method} /api/{action}?{query}` با `body` اصلی برمی‌گرداند؛ پس احراز هویت و دسترسی همان سرویس مقصد اعمال می‌شود.
UI و PublicPortal این body را خودکار می‌سازند و کد کنترلرها تغییری نکرده است.

- body بدون `action` یا JSON نامعتبر ← `400`؛ `method` که با تعریف سرویس جور نباشد ← `400` (همراه فهرست متدهای مجاز).
- فراخوانی هر هندلر **بدون body** فهرست سرویس‌های پشت آن را (با متد اصلی هر کدام) برمی‌گرداند — برای تست اتصال مناسب است.
- شکل قدیمی (`?action=...` در query با body خام و هدر `X-HTTP-Method-Override`) برای سازگاری هنوز پذیرفته می‌شود، ولی پشت Pomix استفاده نکنید.

## حالت‌های ارتباط (Pomix / DirectGateway / DirectApi)

| حالت | مسیر | کاربرد |
|---|---|---|
| `Pomix` | `POST {PomixUrls:<handler>}` با body بالا، از طریق Gateway سازمان | حالت عادی روی سرور |
| `DirectGateway` | `POST {URL}/api/<handler>` با همان body، مستقیم به سرور API | تست، یا وقتی Gateway سازمان مشکل دارد |
| `DirectApi` | `{URL}/api/Auth/login` و ... (همه‌ی اندپوینت‌ها) | فقط عیب‌یابی در شبکه‌ی داخلی |

**سمت UI و PublicPortal** (`appsettings.json`) فقط `Mode` را عوض کنید؛ آدرس هر سه حالت از قبل در کانفیگ است:

```json
"ApiSettings": {
  "Mode": "Pomix",
  "URL": "https://{ip-داخلی-api}:5066",
  "PomixUrls": {
    "auth-handler": "https://{pomix}/.../auth-handler",
    "ehraz-handler": "https://{pomix}/.../ehraz-handler",
    "service-handler": "https://{pomix}/.../service-handler"
  }
}
```

**سمت API** فقط در `appsettings.json` (بخش `Gateway`) مشخص می‌شود کدام حالت‌ها پذیرفته شوند (جدا با کاما). این مقدار را در `appsettings.Production.json` تکرار نکنید، وگرنه روی سرور آن را override می‌کند:

```json
"Gateway": {
  "AllowedModes": "Pomix, DirectGateway",
  "PomixSourceIps": "10.x.x.x, 10.x.x.y"
}
```

- بدون `DirectApi`، مسیرهای مستقیم (`/api/Auth/login` و ...) `404` می‌دهند.
- بدون `DirectGateway`، هندلرها فقط از IP های `PomixSourceIps` (IP واقعی اتصال سرور Gateway سازمان، نه هدر `X-Forwarded-For`) پذیرفته می‌شوند و بقیه `403` می‌گیرند.
- پیش‌فرض `"Pomix, DirectGateway"` است (DirectApi بسته). برای عیب‌یابی داخلی موقتاً `DirectApi` را اضافه کنید.
- همه‌ی این مقادیر (هر دو سمت) در هر درخواست از کانفیگ خوانده می‌شوند، پس **بدون restart** اعمال می‌شوند.
- **اگر فایل خراب ذخیره شود** (مثلاً یک کاما جا بیفتد):
  - موقع بالا آمدن: برنامه اجرا نمی‌شود و در لاگ نام فایل خراب آمده است.
  - موقع اجرا: هر سه پروژه تا اصلاح فایل همه‌ی درخواست‌ها را با `503` و پیام «خطا در فایل تنظیمات» رد می‌کنند و جزئیات خطا (فایل، شماره‌ی خط) در لاگ با سطح Critical ثبت می‌شود.
    بعد از اصلاح فایل، خودکار و بدون restart به حالت عادی برمی‌گردد. (این برای `appsettings.Secrets.json` هم برقرار است.)
- آدرس و پورت خود API (بخش `Kestrel`) فقط موقع بالا آمدن خوانده می‌شود؛ تغییرش restart لازم دارد.

### شرایط اضطراری (نفوذ یا سوءاستفاده)

1. در API: `"AllowedModes": "Pomix"` و `PomixSourceIps` را پر کنید؛ فقط Gateway سازمان می‌تواند API را صدا بزند.
2. `ApiKey` را در `appsettings.Secrets.json` هر سه پروژه با مقدار جدید عوض کنید؛ درخواست‌های با کلید قدیمی فوراً `401` می‌گیرند.
3. در صورت نیاز، از سازمان بخواهید آدرس‌ها را در Gateway موقتاً ببندد.

## امنیت و رمزها

- **X-API-Key:** API هر درخواست به `/api/*` را بدون هدر `X-API-Key` معتبر با `401` رد می‌کند. UI و PublicPortal این هدر را خودکار می‌فرستند.
  در Swagger هم قبل از "Try it out" باید از دکمه‌ی Authorize مقدار ApiKey را وارد کرد.
- **CORS:** در `appsettings.json` پروژه‌ی API، بخش `Cors` (`Enabled` و `AllowedOrigins` جدا با کاما)؛ بدون restart اعمال می‌شود.
  فقط وقتی لازم است که مرورگر مستقیم API را صدا بزند — UI و PublicPortal سرور به سرور وصل می‌شوند و به آن نیاز ندارند،
  پس روی سرور امن‌ترین حالت `"Enabled": false` است. بدون این کلید هم پیش‌فرض خاموش است.
- **مسیرهای مستقیم و حالت‌ها:** با `Gateway:AllowedModes` در API کنترل می‌شود (بخش «حالت‌های ارتباط» بالا).
- **رمزها:** کلیدها و رمزها (ApiKey، Jwt:Key، Encryption، شاهکار، bsr، حساب سرویس پورتال) در `appsettings.json` نیستند؛
  هر پروژه آن‌ها را از `appsettings.Secrets.json` کنار برنامه می‌خواند (در گیت نیست). ساختار فایل در
  `appsettings.Secrets.example.json` هر پروژه آمده. به‌جای فایل می‌شود از متغیر محیطی هم استفاده کرد (مثلاً `Jwt__Key`).
  - `ApiKey` در API باید با `ApiSettings:ApiKey` در UI و PublicPortal یکی باشد.
  - `Encryption:Key` و `Encryption:HmacKey` را **عوض نکنید**؛ داده‌های فعلی دیتابیس با همین‌ها رمز شده‌اند.
- **محیط:** روی سرور هر سه پروژه باید `ASPNETCORE_ENVIRONMENT=Production` باشند؛ در `Development` کد OTP در پاسخ برمی‌گردد و پیامک واقعی ارسال نمی‌شود.
