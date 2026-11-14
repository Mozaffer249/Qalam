using System.Net;

namespace Qalam.Service.Implementations.LRS.Building;

/// <summary>
/// Dynamic data for one completion certificate. All values are raw (un-encoded);
/// <see cref="CertificateHtmlBuilder"/> HTML-encodes everything before rendering.
/// </summary>
public sealed class CertificateModel
{
    public string LearnerName { get; init; } = "—";
    public string CourseTitle { get; init; } = "—";
    public string? InstructorName { get; init; }
    public string? DomainNameAr { get; init; }
    public string? DomainNameEn { get; init; }
    public DateTime IssuedOn { get; init; } = DateTime.UtcNow;
    public int CertificateId { get; init; }

    /// <summary>Public, self-referential URL of this certificate page (used for the verification box + QR).</summary>
    public string VerificationUrl { get; init; } = "";

    public string PlatformNameAr { get; init; } = "قلم";
    public string PlatformNameEn { get; init; } = "Qalam";
}

/// <summary>
/// Builds a premium, print-ready (A4 landscape) bilingual HTML completion certificate.
/// Self-contained: inline CSS + inline SVG branding/seal, Google Fonts with system fallbacks,
/// and a client-side QR that degrades gracefully to a selectable verification link.
/// Reusable and pure — hand it a <see cref="CertificateModel"/>, get HTML back.
/// </summary>
public static class CertificateHtmlBuilder
{
    public static string Build(CertificateModel m)
    {
        string learner = Enc(m.LearnerName);
        string course = Enc(m.CourseTitle);
        string instructor = Enc(string.IsNullOrWhiteSpace(m.InstructorName) ? null : m.InstructorName);
        string platformAr = Enc(m.PlatformNameAr);
        string platformEn = Enc(m.PlatformNameEn);
        string certNo = $"QLM-{m.CertificateId:D6}";
        string issuedAr = m.IssuedOn.ToString("yyyy/MM/dd");
        string issuedEn = m.IssuedOn.ToString("dd MMM yyyy", System.Globalization.CultureInfo.InvariantCulture);
        string verifyUrl = Enc(m.VerificationUrl);
        string verifyUrlJs = JsString(m.VerificationUrl);

        string domainAr = Enc(string.IsNullOrWhiteSpace(m.DomainNameAr) ? null : m.DomainNameAr);
        string domainEn = Enc(string.IsNullOrWhiteSpace(m.DomainNameEn) ? null : m.DomainNameEn);
        string domainChip = (domainAr != null || domainEn != null)
            ? $@"<div class=""chip"">{domainAr}{(domainAr != null && domainEn != null ? " · " : "")}{domainEn}</div>"
            : "";

        string rightColumn = instructor != null
            ? $@"<div class=""sig sig--instructor"">
                   <div class=""sig-name"" dir=""auto"">{instructor}</div>
                   <div class=""sig-rule""></div>
                   <div class=""sig-role"">المعلّم / Instructor</div>
                 </div>"
            : $@"<div class=""sig sig--authority"">
                   <div class=""sig-name"" dir=""auto"">{platformAr}</div>
                   <div class=""sig-rule""></div>
                   <div class=""sig-role"">الجهة المانحة / Issuing Authority</div>
                 </div>";

        return $@"<!doctype html>
<html lang=""ar"" dir=""rtl"">
<head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1"">
<title>شهادة إتمام — {course}</title>
<meta name=""description"" content=""شهادة إتمام معتمدة من منصة {platformAr} · Certificate of Completion from {platformEn}"">
<link rel=""preconnect"" href=""https://fonts.googleapis.com"">
<link rel=""preconnect"" href=""https://fonts.gstatic.com"" crossorigin>
<link href=""https://fonts.googleapis.com/css2?family=Amiri:wght@400;700&family=Cairo:wght@400;600;700;900&family=Cormorant+Garamond:ital,wght@0,500;0,600;1,500&family=Inter:wght@400;500;600&display=swap"" rel=""stylesheet"">
<style>
  :root {{
    --navy: #0f2742;
    --navy-deep: #091a30;
    --gold: #c8a24a;
    --gold-soft: #e7d4a0;
    --gold-deep: #a7832f;
    --ink: #203049;
    --muted: #6a7589;
    --paper: #fbfaf6;
    --paper-edge: #f1ece0;
    --line: rgba(15,39,66,.14);
  }}
  * {{ box-sizing: border-box; }}
  html, body {{ margin: 0; padding: 0; }}
  body {{
    background:
      radial-gradient(1200px 600px at 50% -10%, #19304e 0%, #0b1d35 55%, #081628 100%);
    color: var(--ink);
    font-family: 'Cairo', 'Segoe UI', Tahoma, system-ui, sans-serif;
    min-height: 100vh;
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 28px 16px 72px;
    -webkit-print-color-adjust: exact;
    print-color-adjust: exact;
  }}

  /* ---- A4 landscape sheet ---------------------------------------------- */
  .sheet {{
    position: relative;
    width: 1123px;            /* A4 landscape @96dpi */
    max-width: 100%;
    aspect-ratio: 1123 / 794;
    background:
      linear-gradient(180deg, var(--paper) 0%, var(--paper-edge) 100%);
    border-radius: 10px;
    box-shadow: 0 40px 80px -30px rgba(0,0,0,.65), 0 2px 0 rgba(255,255,255,.5) inset;
    overflow: hidden;
    display: flex;
    flex-direction: column;
    container-type: inline-size;
  }}
  /* Subtle guilloché / watermark */
  .sheet::before {{
    content: '';
    position: absolute; inset: 0;
    background-image:
      repeating-radial-gradient(circle at 50% 46%, rgba(15,39,66,.025) 0 2px, transparent 2px 7px);
    pointer-events: none;
  }}
  .wm {{
    position: absolute; inset: 0;
    display: grid; place-items: center;
    pointer-events: none;
    opacity: .05;
  }}
  .wm svg {{ width: 46%; height: auto; }}

  /* Ornamental gold frame */
  .frame {{
    position: absolute; inset: 20px;
    border: 1.5px solid var(--gold);
    border-radius: 6px;
    pointer-events: none;
  }}
  .frame::after {{
    content: '';
    position: absolute; inset: 6px;
    border: 1px solid rgba(200,162,74,.4);
    border-radius: 4px;
  }}
  .corner {{ position: absolute; width: 58px; height: 58px; color: var(--gold-deep); }}
  .corner svg {{ width: 100%; height: 100%; }}
  .corner.tl {{ top: 14px; left: 14px; }}
  .corner.tr {{ top: 14px; right: 14px; transform: scaleX(-1); }}
  .corner.bl {{ bottom: 14px; left: 14px; transform: scaleY(-1); }}
  .corner.br {{ bottom: 14px; right: 14px; transform: scale(-1,-1); }}

  /* ---- Header ---------------------------------------------------------- */
  .head {{
    position: relative;
    display: flex; align-items: center; justify-content: space-between;
    gap: 16px;
    padding: 40px 64px 0;
  }}
  .brand {{ display: flex; align-items: center; gap: 14px; }}
  .brand .mark {{
    width: 48px; height: 48px; flex: none;
    display: grid; place-items: center;
    border-radius: 12px;
    background: linear-gradient(145deg, var(--navy), var(--navy-deep));
    box-shadow: 0 6px 16px -6px rgba(15,39,66,.6), 0 0 0 1px rgba(200,162,74,.5) inset;
    color: var(--gold-soft);
  }}
  .brand .mark svg {{ width: 26px; height: 26px; }}
  .brand .names {{ line-height: 1.05; }}
  .brand .n-ar {{ font-family: 'Amiri', serif; font-weight: 700; font-size: 26px; color: var(--navy); }}
  .brand .n-en {{ font-family: 'Inter', sans-serif; font-size: 11px; letter-spacing: .28em; text-transform: uppercase; color: var(--muted); }}
  .seal-no {{ text-align: center; direction: ltr; }}
  .seal-no .lbl {{ font-family: 'Inter', sans-serif; font-size: 9px; letter-spacing: .24em; text-transform: uppercase; color: var(--muted); }}
  .seal-no .val {{ font-family: 'Inter', sans-serif; font-weight: 600; font-size: 14px; color: var(--navy); letter-spacing: .06em; }}

  /* ---- Body ------------------------------------------------------------ */
  .body {{
    position: relative;
    flex: 1;
    display: flex; flex-direction: column; align-items: center; justify-content: center;
    text-align: center;
    padding: 6px 64px 0;
  }}
  .title {{
    font-family: 'Amiri', serif; font-weight: 700;
    font-size: 40px; color: var(--navy);
    letter-spacing: .01em;
    margin: 0;
  }}
  .title-en {{
    font-family: 'Cormorant Garamond', serif; font-style: italic;
    font-size: 17px; color: var(--gold-deep);
    letter-spacing: .14em; text-transform: uppercase;
    margin-top: 2px;
  }}
  .flourish {{ display: flex; align-items: center; gap: 12px; margin: 14px 0 20px; color: var(--gold); }}
  .flourish .line {{ width: 90px; height: 1px; background: linear-gradient(90deg, transparent, var(--gold)); }}
  .flourish .line.r {{ background: linear-gradient(90deg, var(--gold), transparent); }}
  .flourish .dot {{ width: 7px; height: 7px; transform: rotate(45deg); background: var(--gold); box-shadow: 0 0 0 3px rgba(200,162,74,.18); }}

  .lead {{ font-size: 15px; color: var(--muted); }}
  .learner {{
    font-family: 'Amiri', serif; font-weight: 700;
    font-size: 52px; line-height: 1.1;
    color: var(--navy-deep);
    margin: 8px 0 6px;
  }}
  .learner-rule {{ width: 340px; max-width: 70%; height: 2px; background: linear-gradient(90deg, transparent, var(--gold), transparent); margin: 0 auto 14px; }}
  .sub {{ font-size: 15px; color: var(--muted); }}
  .course {{
    font-family: 'Cormorant Garamond', 'Amiri', serif; font-weight: 600;
    font-size: 30px; color: var(--navy);
    margin: 6px 0 10px; max-width: 760px;
  }}
  .chip {{
    display: inline-block;
    font-size: 12px; font-weight: 600; color: var(--gold-deep);
    padding: 5px 16px; border-radius: 999px;
    background: rgba(200,162,74,.1);
    border: 1px solid rgba(200,162,74,.4);
  }}

  /* ---- Footer ---------------------------------------------------------- */
  .foot {{
    position: relative;
    display: grid;
    grid-template-columns: 1fr auto 1fr;
    align-items: end;
    gap: 20px;
    padding: 0 64px 44px;
  }}
  .sig {{ text-align: center; min-width: 190px; }}
  .sig-name {{ font-family: 'Amiri', serif; font-weight: 700; font-size: 18px; color: var(--navy); min-height: 24px; }}
  .sig-rule {{ height: 1px; background: var(--line); margin: 6px 0; }}
  .sig-role {{ font-size: 11px; letter-spacing: .04em; color: var(--muted); }}
  .sig--date {{ direction: ltr; }}

  .medallion {{ display: flex; flex-direction: column; align-items: center; gap: 8px; }}
  .medallion svg {{ width: 104px; height: 104px; }}
  .verify {{ display: flex; flex-direction: column; align-items: center; gap: 6px; }}
  .qr {{ width: 64px; height: 64px; background: #fff; border: 1px solid var(--line); border-radius: 6px; padding: 4px; display: grid; place-items: center; }}
  .qr img, .qr canvas {{ width: 100%; height: 100%; display: block; }}
  .verify .vl {{ font-size: 9px; letter-spacing: .2em; text-transform: uppercase; color: var(--muted); }}

  .issued-band {{
    grid-column: 1 / -1;
    display: flex; align-items: center; justify-content: space-between;
    gap: 20px; flex-wrap: wrap;
    margin-top: 10px; padding-top: 14px;
    border-top: 1px solid var(--line);
    font-size: 11px; color: var(--muted);
  }}
  .issued-band .verify {{ flex: none; }}
  .issued-band .issued-text {{ display: flex; flex-direction: column; gap: 3px; text-align: start; }}
  .issued-band .url {{ direction: ltr; font-family: 'Inter', monospace; color: var(--navy); word-break: break-all; }}

  /* ---- Print toolbar (screen only) ------------------------------------ */
  .toolbar {{
    position: fixed; inset-inline-end: 20px; inset-block-end: 20px;
    display: flex; gap: 10px; z-index: 50;
  }}
  .toolbar button {{
    font-family: 'Cairo', sans-serif; font-weight: 700; font-size: 13px;
    color: #fff; cursor: pointer;
    padding: 11px 20px; border: 0; border-radius: 999px;
    background: linear-gradient(145deg, var(--gold), var(--gold-deep));
    box-shadow: 0 10px 24px -8px rgba(167,131,47,.7);
  }}
  .toolbar button:active {{ transform: translateY(1px); }}

  /* ---- Responsive preview --------------------------------------------- */
  @media (max-width: 1180px) {{
    .sheet {{ width: 100%; }}
  }}
  @container (max-width: 820px) {{
    .head, .body, .foot {{ padding-inline: 34px; }}
    .title {{ font-size: 7cqw; }}
    .learner {{ font-size: 8.5cqw; }}
    .course {{ font-size: 4.6cqw; }}
  }}
  @media (max-width: 640px) {{
    body {{ padding: 14px 10px 72px; }}
    .foot {{ grid-template-columns: 1fr; justify-items: center; gap: 22px; }}
    .issued-band {{ justify-content: center; text-align: center; }}
  }}

  /* ---- A4 landscape print / PDF --------------------------------------- */
  @page {{ size: A4 landscape; margin: 0; }}
  @media print {{
    body {{ background: #fff; padding: 0; display: block; }}
    .toolbar {{ display: none !important; }}
    .sheet {{
      width: 100%; max-width: none; height: 100vh; aspect-ratio: auto;
      border-radius: 0; box-shadow: none;
    }}
  }}
</style>
</head>
<body>
  <div class=""sheet"" role=""document"" aria-label=""Certificate of completion"">
    <!-- watermark -->
    <div class=""wm"" aria-hidden=""true"">{PenMark()}</div>

    <!-- gold frame + corners -->
    <div class=""frame""></div>
    <span class=""corner tl"" aria-hidden=""true"">{Corner()}</span>
    <span class=""corner tr"" aria-hidden=""true"">{Corner()}</span>
    <span class=""corner bl"" aria-hidden=""true"">{Corner()}</span>
    <span class=""corner br"" aria-hidden=""true"">{Corner()}</span>

    <!-- header -->
    <header class=""head"">
      <div class=""brand"">
        <span class=""mark"" aria-hidden=""true"">{PenMark()}</span>
        <span class=""names"">
          <span class=""n-ar"">{platformAr}</span><br>
          <span class=""n-en"">{platformEn} · Learning</span>
        </span>
      </div>
      <div class=""seal-no"">
        <div class=""lbl"">Certificate No.</div>
        <div class=""val"">{certNo}</div>
      </div>
    </header>

    <!-- body -->
    <main class=""body"">
      <h1 class=""title"">شهادة إتمام</h1>
      <div class=""title-en"">Certificate of Completion</div>
      <div class=""flourish"" aria-hidden=""true"">
        <span class=""line""></span><span class=""dot""></span><span class=""line r""></span>
      </div>

      <p class=""lead"">تشهد منصة {platformAr} للتعليم بأن</p>
      <div class=""learner"" dir=""auto"">{learner}</div>
      <div class=""learner-rule""></div>

      <p class=""sub"">قد أتمّ / أتمّت بنجاح الدورة التدريبية · has successfully completed</p>
      <div class=""course"" dir=""auto"">{course}</div>
      {domainChip}
    </main>

    <!-- footer -->
    <footer class=""foot"">
      <div class=""sig sig--date"">
        <div class=""sig-name"">{issuedEn}</div>
        <div class=""sig-rule""></div>
        <div class=""sig-role"">تاريخ الإصدار / Issued · {issuedAr}</div>
      </div>

      <div class=""medallion"" aria-hidden=""true"">
        {Medallion()}
      </div>

      {rightColumn}

      <div class=""issued-band"">
        <div class=""verify"">
          <div class=""qr"" id=""qr"" title=""{verifyUrl}""></div>
          <div class=""vl"">Scan to verify</div>
        </div>
        <div class=""issued-text"">
          <span>شهادة معتمدة إلكترونيًا من منصة {platformAr} · Digitally verified by {platformEn}</span>
          <span class=""url"">{verifyUrl}</span>
        </div>
      </div>
    </footer>
  </div>

  <div class=""toolbar"">
    <button type=""button"" onclick=""window.print()"">طباعة / حفظ PDF</button>
  </div>

  <script src=""https://cdnjs.cloudflare.com/ajax/libs/qrcodejs/1.0.0/qrcode.min.js""></script>
  <script>
    (function () {{
      var url = {verifyUrlJs};
      var host = document.getElementById('qr');
      if (host && url && window.QRCode) {{
        try {{
          new QRCode(host, {{ text: url, width: 120, height: 120, correctLevel: QRCode.CorrectLevel.M }});
        }} catch (e) {{ /* fallback: link text remains visible */ }}
      }}
    }})();
  </script>
</body>
</html>";
    }

    // ---- inline SVG assets (self-contained branding) ----
    private static string PenMark() => @"<svg viewBox=""0 0 24 24"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"">
        <path d=""M4 20l3-1 11-11a2.5 2.5 0 0 0-3.5-3.5L3.5 15.5 2.5 19 4 20z"" fill=""currentColor"" opacity="".92""/>
        <path d=""M13.5 6.5l4 4"" stroke=""#fff"" stroke-opacity="".35"" stroke-width=""1.2""/>
        <path d=""M2.6 19l2.4-.9 .9 2.4"" fill=""#fff"" fill-opacity="".9""/></svg>";

    private static string Corner() => @"<svg viewBox=""0 0 58 58"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"">
        <path d=""M4 54V18C4 10 10 4 18 4h36"" stroke=""currentColor"" stroke-width=""1.4""/>
        <path d=""M12 54V22c0-6 4-10 10-10h32"" stroke=""currentColor"" stroke-width="".8"" opacity="".6""/>
        <circle cx=""18"" cy=""18"" r=""3"" fill=""currentColor""/></svg>";

    private static string Medallion() => @"<svg viewBox=""0 0 120 120"" xmlns=""http://www.w3.org/2000/svg"">
        <defs>
          <linearGradient id=""g"" x1=""0"" y1=""0"" x2=""1"" y2=""1"">
            <stop offset=""0"" stop-color=""#e7d4a0""/><stop offset="".5"" stop-color=""#c8a24a""/><stop offset=""1"" stop-color=""#a7832f""/>
          </linearGradient>
        </defs>
        <g transform=""translate(60 50)"">
          <polygon points=""0,46 10,70 0,64 -10,70"" fill=""#a7832f""/>
          <polygon points=""0,46 -4,74 -14,70 -10,46"" fill=""#c8a24a"" opacity="".9""/>
        </g>
        <circle cx=""60"" cy=""46"" r=""36"" fill=""url(#g)""/>
        <circle cx=""60"" cy=""46"" r=""36"" fill=""none"" stroke=""#8a6a24"" stroke-width=""1.2""/>
        <circle cx=""60"" cy=""46"" r=""28"" fill=""none"" stroke=""#fff"" stroke-opacity="".5"" stroke-width=""1"" stroke-dasharray=""2 3""/>
        <path d=""M60 30l4.2 8.5 9.4 1.4-6.8 6.6 1.6 9.3L60 59l-8.4 4.4 1.6-9.3-6.8-6.6 9.4-1.4z"" fill=""#fff"" fill-opacity="".92""/>
      </svg>";

    private static string Enc(string? s) =>
        s == null ? null! : WebUtility.HtmlEncode(s);

    private static string JsString(string? s)
    {
        s ??= "";
        var sb = new System.Text.StringBuilder(s.Length + 2);
        sb.Append('"');
        foreach (var c in s)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '<': sb.Append("\\u003C"); break;
                case '>': sb.Append("\\u003E"); break;
                case '&': sb.Append("\\u0026"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                default: sb.Append(c); break;
            }
        }
        sb.Append('"');
        return sb.ToString();
    }
}
