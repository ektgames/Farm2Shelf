namespace Farm2Shelf.Core
{
    // Fourteen recordings per language: three fixed and eleven rotating.
    public static class StoreAnnouncementCatalog
    {
        public sealed class Entry
        {
            public readonly string Id, Turkish, English;
            public readonly int FromHour, UntilHour;
            public Entry(string id, int from, int until, string tr, string en)
            { Id = id; FromHour = from; UntilHour = until; Turkish = tr; English = en; }
        }

        public static readonly Entry[] Entries =
        {
            new Entry("opening", 8, 9, "Günaydın, değerli müşterilerimiz. Mağazamız açılmıştır. Hepinize güzel bir gün ve keyifli alışverişler dileriz.", "Good morning, dear customers. Our store is now open. We wish you a lovely day and a pleasant shopping experience."),
            new Entry("closing_soon", 22, 23, "Değerli müşterilerimiz, mağazamız kısa süre sonra kapanacaktır. Alışverişinizi tamamlayarak kasalarımıza yönelmenizi rica ederiz. Anlayışınız için teşekkür ederiz.", "Dear customers, our store will be closing soon. Please finish your shopping and make your way to the checkouts. Thank you for your understanding."),
            new Entry("closed", 24, 25, "Değerli müşterilerimiz, mağazamız kapanmıştır. Bugün bizi tercih ettiğiniz için teşekkür eder, iyi geceler dileriz. Yarın yeniden görüşmek üzere.", "Dear customers, our store is now closed. Thank you for shopping with us today. Have a lovely evening, and we look forward to seeing you again tomorrow."),
            new Entry("lost_property", 10, 22, "Küçük bir hatırlatma: Mağazamızda bir eşyanızı kaybettiyseniz lütfen bir görevlimize haber verin. Bulduğunuz eşyaları da çalışanlarımıza teslim edebilirsiniz.", "A gentle reminder: if you have misplaced something in our store, please let a member of staff know. You can also hand any items you find to our team."),
            new Entry("parking", 10, 22, "Araçlarıyla gelen müşterilerimizden, park çizgilerine dikkat etmelerini ve giriş çıkış yollarını açık bırakmalarını rica ederiz. İlginiz için teşekkür ederiz.", "Customers arriving by car, please park within the marked spaces and keep entrances and exits clear. Thank you for your consideration."),
            new Entry("midday", 12, 16, "Öğle saatlerinde aramıza katılan tüm müşterilerimize hoş geldiniz diyoruz. Reyonlarda ve kasalarda birbirimize alan bırakarak alışverişimizi daha keyifli hale getirebiliriz.", "A warm welcome to everyone joining us this lunchtime. Please leave a little space for others in the aisles and at the checkouts. Thank you."),
            new Entry("belongings", 10, 22, "Alışverişiniz sırasında çanta, telefon ve cüzdanınızı yanınızda bulundurmayı unutmayın. Mağazadan ayrılırken kişisel eşyalarınızı kontrol etmenizi öneririz.", "Please remember to keep your bag, phone and wallet with you while shopping. Before you leave, take a moment to check you have all your belongings."),
            new Entry("children", 10, 22, "Minik misafirlerimize ve ailelerine hoş geldiniz. Çocuklarımızın güvenliği için alışveriş boyunca yanınızda kalmalarını rica ederiz. Keyifli alışverişler.", "A warm welcome to our younger visitors and their families. For their safety, please keep children close to you throughout your visit. Enjoy your shopping."),
            new Entry("aisles", 10, 22, "Değerli müşterilerimiz, alışveriş sepetlerinizi geçişleri kapatmayacak şekilde tutmanızı rica ederiz. Herkesin rahatça hareket edebilmesi için gösterdiğiniz özen için teşekkürler.", "Dear customers, please keep your shopping baskets clear of the walkways. Thank you for helping everyone move comfortably around the store."),
            new Entry("fragile", 10, 22, "Cam ambalajlı ürünleri incelerken dikkatli olmanızı rica ederiz. Bir ürün yere düşerse lütfen görevlimize haber verin; size yardımcı olalım.", "Please take care when handling products in glass packaging. If an item falls, let a member of staff know so we can help."),
            new Entry("queues", 12, 22, "Kasalarımızda sıra bekleyen müşterilerimize sabırları için teşekkür ederiz. Önünüzdeki müşteriye biraz alan bırakmanız işlemlerin daha rahat ilerlemesine yardımcı olur.", "Thank you for your patience at the checkouts. Please allow a little space for the customer ahead of you so everyone can check out comfortably."),
            new Entry("ask_staff", 10, 22, "Aradığınız ürünü bulmakta zorlanıyorsanız çalışanlarımıza danışabilirsiniz. Size yardımcı olmaktan mutluluk duyarız. İyi alışverişler.", "If you need help finding a product, please ask a member of our team. We will be happy to help. Enjoy your shopping."),
            new Entry("cold_products", 10, 22, "Soğuk ürün alışverişinizde küçük bir öneri: Buzdolabından aldığınız ürünleri alışverişinizin sonuna bırakmanız, eve kadar serin kalmalarına yardımcı olur.", "A little shopping tip: picking up chilled products towards the end of your visit helps keep them cool on the journey home."),
            new Entry("clean_store", 10, 22, "Temiz ve rahat bir alışveriş ortamı için çöplerimizi yerde bırakmayalım. Yerde dökülmüş bir ürün görürseniz lütfen çalışanlarımıza haber verin. Teşekkür ederiz.", "Please help us keep the store clean and comfortable by keeping litter off the floor. If you spot a spill, please let our team know. Thank you."),
        };
    }
}
