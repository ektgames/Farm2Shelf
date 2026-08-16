# Farm2Shelf Proje Kuralları

1. **Eksiksiz ve Yüksek Kalite**: 
   - Tüm geliştirmeler tam, temiz ve üretime hazır (production-ready) kalitede yapılacaktır. Baştan savma veya yarım bırakılmış kod/placeholder yazılmayacaktır.
   - Mimari ve kodlama standartları Unity en iyi pratiklerine (Best Practices) uygun olacaktır.

2. **Tak-Çalıştır (Play Testi)**:
   - Yazılan tüm sistemler kullanıcının Unity editöründe sadece **Play** butonuna basarak doğrudan test edebileceği şekilde (ör. eksik bağımlılık bırakmadan, `RequireComponent`, dinamik kurucular veya kurulum yardımcıları ile) hazırlanacaktır.

3. **Dil Gereksinimi**:
   - Bütün uygulama planları (`implementation_plan.md`), adım açıklamaları, yürütme raporları ve tüm yanıtlar **Türkçe** verilecektir.

4. **Mevcut Yapıyı Koruma (Düzen & Mimari Bütünlük)**:
   - Gelecekte eklenecek yeni özelliklerde veya hata düzeltmelerinde (bug fix), mevcut onaylanmış harita mimarisi (çift turnikeli otopark kapısı, hizalı yaya geçitleri, dükkan/otopark seviye derinlikleri, çevre yolları ve kaldırımlar) KESİNLİKLE bozulmayacak ve değiştirilmeyecektir.

5. **Mobil ve PC Çapraz Uyumluluk (Mobile-First & Hybrid Input)**:
   - Geliştirilen tüm sistemler, UI butonları, dokunmatik kontroller, 2D ikonlar, koli etkileşimleri ve önizleme/yerleştirme mekanizmaları hem mobil (Android/iOS Dokunmatik / Pinch-Zoom / Touch) hem de PC (Klavye/Fare) için %100 uyumlu, ölçeklenebilir ve erişilebilir olacaktır.
