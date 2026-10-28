using System.Collections.Generic;
using UnityEngine;
using Farm2Shelf.Core;
using Farm2Shelf.Utils;

namespace Farm2Shelf.Environment
{
    public static class ProceduralStaffModelBuilder
    {
        private static readonly Dictionary<string, Material> matCache = new Dictionary<string, Material>();

        private static Material GetMaterial(string name, Color color, float metallic = 0.1f, float smoothness = 0.4f)
        {
            if (matCache.TryGetValue(name, out Material mat) && mat != null)
            {
                return mat;
            }

            Shader shader = ShaderHelper.GetLitShader();
            if (shader == null) shader = Shader.Find("Standard");

            Material newMat = new Material(shader)
            {
                name = name,
                color = color
            };

            if (newMat.HasProperty("_BaseColor")) newMat.SetColor("_BaseColor", color);
            if (newMat.HasProperty("_Color")) newMat.SetColor("_Color", color);
            if (newMat.HasProperty("_Metallic")) newMat.SetFloat("_Metallic", metallic);
            if (newMat.HasProperty("_Smoothness")) newMat.SetFloat("_Smoothness", smoothness);

            matCache[name] = newMat;
            return newMat;
        }

        public static GameObject CreateStaffCharacterModel(StaffRole role, bool isFemale, out List<Transform> leftLimbs, out List<Transform> rightLimbs)
        {
            return CreateStaffModel(role, isFemale, out leftLimbs, out rightLimbs);
        }

        public static GameObject CreateStaffModel(StaffRole role, bool isFemale, out List<Transform> leftLimbs, out List<Transform> rightLimbs)
        {
            leftLimbs = new List<Transform>();
            rightLimbs = new List<Transform>();

            string genderPrefix = isFemale ? "Female_" : "Male_";
            GameObject root = new GameObject($"Staff_{genderPrefix}{role}");
            Transform tRoot = root.transform;

            // Fiziksel Çarpışma ve Kapı/Tıklama Algılama Bileşenleri (Mobil Dokunmatik Uyumlu Geniş Alan)
            CapsuleCollider col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 1.0f, 0f);
            col.radius = 0.55f;
            col.height = 2.0f;
            col.isTrigger = true;

            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            if(role==StaffRole.Maskot)
            {
                BuildPlushMascot(tRoot,isFemale,leftLimbs,rightLimbs);
                col.height=isFemale?2.65f:2.25f;col.center=Vector3.up*(col.height*.5f);col.radius=.44f;
                return root;
            }

            // Ten & Saç Materyalleri
            Material skinMat = isFemale ? GetMaterial("Mat_StaffSkinF", new Color(0.95f, 0.80f, 0.68f)) : GetMaterial("Mat_StaffSkinM", new Color(0.88f, 0.72f, 0.58f));
            Material hairMat = isFemale ? GetMaterial("Mat_StaffHairF", new Color(0.35f, 0.22f, 0.14f)) : GetMaterial("Mat_StaffHairM", new Color(0.12f, 0.12f, 0.14f));
            Material shoeMat = GetMaterial("Mat_StaffShoe", new Color(0.12f, 0.12f, 0.14f));

            // Role Özel Üniforma Renkleri
            Color uniformPrimary = GetRolePrimaryColor(role);
            Color uniformPants = GetRolePantsColor(role);

            Material torsoMat = GetMaterial($"Mat_StaffTorso_{role}", uniformPrimary);
            Material pantsMat = GetMaterial($"Mat_StaffPants_{role}", uniformPants);

            // Anatomi Boyutları (Erkek vs Kadın)
            Vector3 torsoSize = isFemale ? new Vector3(0.44f, 0.52f, 0.28f) : new Vector3(0.52f, 0.55f, 0.30f);
            Vector3 headSize = isFemale ? new Vector3(0.30f, 0.30f, 0.28f) : new Vector3(0.32f, 0.32f, 0.30f);

            // Torso (Gövde)
            CreateBlock(tRoot, "Torso", new Vector3(0f, 1.05f, 0f), torsoSize, torsoMat);

            // Baş (Head)
            GameObject headObj = CreateBlock(tRoot, "Head", new Vector3(0f, 1.50f, 0f), headSize, skinMat);

            // Sol & Sağ Bacak
            float legX = isFemale ? 0.13f : 0.15f;
            GameObject lLeg = CreateSeatedServiceLeg(tRoot,"Leg_L",-legX,pantsMat,shoeMat);
            GameObject rLeg = CreateSeatedServiceLeg(tRoot,"Leg_R",legX,pantsMat,shoeMat);
            leftLimbs.Add(lLeg.transform);
            rightLimbs.Add(rLeg.transform);

            // Sol & Sağ Kol
            float armX = isFemale ? 0.28f : 0.32f;
            GameObject lArm = CreateArm(tRoot, "Arm_L", new Vector3(-armX, 1.25f, 0f), new Vector3(0.15f, 0.55f, 0.16f), torsoMat, skinMat);
            GameObject rArm = CreateArm(tRoot, "Arm_R", new Vector3(armX, 1.25f, 0f), new Vector3(0.15f, 0.55f, 0.16f), torsoMat, skinMat);
            leftLimbs.Add(lArm.transform);
            rightLimbs.Add(rArm.transform);

            bool skipCostumeFace = role == StaffRole.Maskot;
            bool skipHairForHelmet = role == StaffRole.Kurye || role == StaffRole.Maskot;
            ProceduralHumanFaceBuilder.HairStyle hairStyle = isFemale
                ? ProceduralHumanFaceBuilder.HairStyle.PonytailFemale
                : ProceduralHumanFaceBuilder.HairStyle.ShortMale;
            bool beard = false;
            bool mustache = false;
            if (!isFemale)
            {
                if (role == StaffRole.Çiftçi || role == StaffRole.DeneyimliÇiftçi || role == StaffRole.UstaÇiftlikSorumlusu)
                {
                    beard = true;
                    mustache = true;
                    hairStyle = ProceduralHumanFaceBuilder.HairStyle.CoveredShort;
                }
                else if (role == StaffRole.Güvenlik)
                {
                    mustache = true;
                    hairStyle = ProceduralHumanFaceBuilder.HairStyle.CoveredShort;
                }
                else if (role == StaffRole.TarımOtomasyonUzmanı)
                {
                    hairStyle = ProceduralHumanFaceBuilder.HairStyle.CoveredShort;
                }
            }
            else if (role == StaffRole.Çiftçi || role == StaffRole.DeneyimliÇiftçi || role == StaffRole.UstaÇiftlikSorumlusu || role == StaffRole.Güvenlik)
            {
                hairStyle = ProceduralHumanFaceBuilder.HairStyle.CoveredShort;
            }

            ProceduralHumanFaceBuilder.BuildOnCharacter(headObj.transform, tRoot, new ProceduralHumanFaceBuilder.FaceSettings
            {
                Skin = skinMat,
                Hair = hairMat,
                IsFemale = isFemale,
                HasBeard = beard,
                HasMustache = mustache,
                HasLipstick = isFemale && role != StaffRole.Maskot,
                SkipHair = skipHairForHelmet,
                SkipFace = skipCostumeFace,
                HairStyle = hairStyle,
                Variant = (int)role + (isFemale ? 3 : 0)
            });

            // Saç ve Cinsiyet Detayı
            BuildHairAndHeadwear(tRoot, isFemale, hairMat, role);

            // Role Özel Üniforma ve Aksesuar Detayları
            BuildRoleUniformAccessories(tRoot, role, isFemale, skinMat);

            return root;
        }

        private static GameObject PlushPart(Transform parent,string name,Vector3 position,Vector3 scale,Material material)
        {
            var part=GameObject.CreatePrimitive(PrimitiveType.Sphere);part.name=name;
            part.transform.SetParent(parent,false);part.transform.localPosition=position;part.transform.localScale=scale;
            part.GetComponent<Renderer>().sharedMaterial=material;
            var collider=part.GetComponent<Collider>();if(collider!=null){collider.enabled=false;Object.Destroy(collider);}return part;
        }
        private static Transform PlushJoint(Transform parent,string name,Vector3 position)
        {
            var joint=new GameObject(name).transform;joint.SetParent(parent,false);joint.localPosition=position;return joint;
        }
        private static void BuildPlushMascot(Transform root,bool bunny,List<Transform> left,List<Transform> right)
        {
            var fur=GetMaterial(bunny?"Mascot_Rose_Plush":"Mascot_Honey_Plush",bunny?new Color(.92f,.58f,.66f):new Color(.65f,.39f,.17f),0,.15f);
            var cream=GetMaterial("Mascot_Vanilla_Plush",new Color(1f,.9f,.72f),0,.12f);
            var ink=GetMaterial("Mascot_Embroidered_Eyes",new Color(.035f,.045f,.055f),0,.32f);
            var white=GetMaterial("Mascot_Eye_Glint",Color.white,0,.5f);
            var green=GetMaterial("Mascot_Farm_Apron",new Color(.045f,.36f,.25f),0,.18f);
            var coral=GetMaterial("Mascot_Coral_Details",new Color(.86f,.23f,.3f),0,.2f);
            var gold=GetMaterial("Mascot_Harvest_Gold",new Color(1f,.68f,.13f),0,.25f);
            PlushPart(root,"Plush_Torso",new Vector3(0,1.04f,0),new Vector3(.68f,.87f,.49f),fur);
            PlushPart(root,"Belly_Patch",new Vector3(0,1.04f,.218f),new Vector3(.47f,.59f,.10f),cream);
            PlushPart(root,"Tail",new Vector3(0,.88f,-.27f),Vector3.one*(bunny?.22f:.16f),bunny?cream:fur);
            var head=PlushJoint(root,"Mascot_Head",new Vector3(0,1.75f,.015f));
            PlushPart(head,"Head_Shell",Vector3.zero,new Vector3(.72f,.67f,.62f),fur);
            for(int side=-1;side<=1;side+=2)
            {
                var ear=PlushJoint(head,"Ear_"+side,new Vector3(side*(bunny?.22f:.29f),bunny?.39f:.28f,-.035f));
                ear.localRotation=Quaternion.Euler(0,0,-side*(bunny?10f:15f));
                PlushPart(ear,"Outer",Vector3.zero,bunny?new Vector3(.18f,.69f,.17f):new Vector3(.27f,.29f,.15f),fur);
                PlushPart(ear,"Inner",new Vector3(0,0,.072f),bunny?new Vector3(.105f,.51f,.045f):new Vector3(.16f,.17f,.045f),cream);
                PlushPart(head,"Muzzle_"+side,new Vector3(side*.10f,-.12f,.30f),new Vector3(.25f,.20f,.17f),cream);
                PlushPart(head,"Eye_"+side,new Vector3(side*.155f,.065f,.295f),new Vector3(.10f,.13f,.055f),ink);
                PlushPart(head,"Glint_"+side,new Vector3(side*.155f-.018f,.09f,.322f),new Vector3(.03f,.035f,.013f),white);
                var eyebrow=PlushPart(head,"Brow_"+side,new Vector3(side*.16f,.16f,.27f),new Vector3(.12f,.028f,.025f),ink);
                eyebrow.transform.localRotation=Quaternion.Euler(0,0,-side*10f);
                PlushPart(head,"Cheek_"+side,new Vector3(side*.25f,-.075f,.25f),new Vector3(.09f,.055f,.025f),coral);
                var hip=PlushJoint(root,side<0?"Leg_L":"Leg_R",new Vector3(side*.18f,.8f,0));
                PlushPart(hip,"Thigh",new Vector3(0,-.18f,0),new Vector3(.27f,.40f,.28f),fur);
                var knee=PlushJoint(hip,"ServiceKnee",new Vector3(0,-.36f,0));
                PlushPart(knee,"Shin",new Vector3(0,-.14f,0),new Vector3(.26f,.34f,.27f),fur);
                PlushPart(knee,"Paw_Boot",new Vector3(0,-.33f,.085f),new Vector3(.32f,.22f,.43f),cream);
                for(int toe=-1;toe<=1;toe++)PlushPart(knee,"Toe_Stitch",new Vector3(toe*.065f,-.30f,.28f),new Vector3(.014f,.045f,.014f),fur);
                var arm=PlushJoint(root,side<0?"Arm_L":"Arm_R",new Vector3(side*.39f,1.36f,0));
                PlushPart(arm,"Sleeve",new Vector3(side*.025f,-.21f,0),new Vector3(.24f,.48f,.25f),fur);
                PlushPart(arm,"Mitten",new Vector3(side*.04f,-.47f,.035f),new Vector3(.27f,.27f,.26f),cream);
                PlushPart(arm,"Thumb",new Vector3(-side*.085f,-.43f,.07f),new Vector3(.12f,.15f,.15f),cream);
                (side<0?left:right).Add(hip);(side<0?left:right).Add(arm);
                var bow=PlushPart(root,"Bow_Loop_"+side,new Vector3(side*.09f,1.40f,.25f),new Vector3(.18f,.13f,.10f),bunny?coral:green);
                bow.transform.localRotation=Quaternion.Euler(0,0,side*20f);
            }
            PlushPart(head,"Nose",new Vector3(0,-.055f,.397f),new Vector3(.105f,.075f,.07f),bunny?coral:ink);
            PlushPart(head,"Smile",new Vector3(0,-.22f,.325f),new Vector3(.13f,.035f,.035f),ink);
            PlushPart(root,"Bow_Knot",new Vector3(0,1.40f,.28f),new Vector3(.09f,.10f,.08f),gold);
            PlushPart(root,"Apron_Bib",new Vector3(0,1.08f,.274f),new Vector3(.34f,.40f,.05f),green);
            PlushPart(root,"Harvest_Badge",new Vector3(0,1.14f,.307f),new Vector3(.13f,.13f,.025f),gold);
            var leaf=PlushPart(root,"Badge_Leaf",new Vector3(.035f,1.22f,.31f),new Vector3(.08f,.035f,.022f),cream);
            leaf.transform.localRotation=Quaternion.Euler(0,0,25);
            PlushPart(root,"Apron_Pocket",new Vector3(0,.98f,.305f),new Vector3(.23f,.105f,.025f),cream);
        }

        private static Color GetRolePrimaryColor(StaffRole role)
        {
            switch (role)
            {
                case StaffRole.Kasiyer: return new Color(0.15f, 0.45f, 0.85f); // Mağaza Mavi Tişört
                case StaffRole.Reyoncu: return new Color(0.18f, 0.65f, 0.35f); // Mağaza Yeşil Önlük
                case StaffRole.Temizlikçi: return new Color(0.20f, 0.60f, 0.85f); // Mavi Temizlik Tulumu
                case StaffRole.Güvenlik: return new Color(0.12f, 0.14f, 0.18f); // Siyah Özel Güvenlik Üniforması
                case StaffRole.MüşteriHizmetlisi: return new Color(0.96f, 0.96f, 0.98f); // Lüks Beyaz Takım Elbise / Beyaz Elbise
                case StaffRole.Maskot: return new Color(0.95f, 0.75f, 0.15f); // Sarı Maskot Kostümü
                case StaffRole.Çiftçi: return new Color(0.45f, 0.35f, 0.20f); // Kahverengi Tulum
                case StaffRole.DeneyimliÇiftçi: return new Color(0.25f, 0.45f, 0.25f); // Yeşil Çiftçi Tulumu
                case StaffRole.UstaÇiftlikSorumlusu: return new Color(0.80f, 0.40f, 0.15f); // Turuncu Tulum
                case StaffRole.TarımOtomasyonUzmanı: return new Color(0.20f, 0.30f, 0.50f); // Mavi Mühendis Ceketi
                case StaffRole.Kurye: return new Color(0.12f, 0.14f, 0.16f); // Şık Siyah/Antrasit Deri Motorcu Montu
                default: return Color.blue;
            }
        }

        private static Color GetRolePantsColor(StaffRole role)
        {
            switch (role)
            {
                case StaffRole.Kasiyer: return new Color(0.15f, 0.18f, 0.25f); // Siyah Kumaş Pantolon
                case StaffRole.Reyoncu: return new Color(0.20f, 0.35f, 0.60f); // Kot Pantolon
                case StaffRole.Güvenlik: return new Color(0.10f, 0.12f, 0.15f); // Siyah Güvenlik Pantolonu
                case StaffRole.MüşteriHizmetlisi: return new Color(0.96f, 0.96f, 0.98f); // Beyaz Kumaş Pantolon / Etek
                case StaffRole.Maskot: return new Color(0.95f, 0.75f, 0.15f);
                case StaffRole.Kurye: return new Color(0.15f, 0.18f, 0.22f); // Güçlendirilmiş Motorcu Kot Pantolonu
                default: return new Color(0.18f, 0.20f, 0.25f);
            }
        }

        private static void BuildHairAndHeadwear(Transform parent, bool isFemale, Material hairMat, StaffRole role)
        {
            if (role == StaffRole.Maskot) return; // Maskot özel kafa kullanır

            if (role == StaffRole.Kurye)
            {
                // 🏍️ KASK: ŞIK, VİZÖRLÜ, AERODİNAMİK TAM MOTOSİKLET KASKI (Full-Face Helmet)
                Material helmetShellMat = GetMaterial("Mat_CourierHelmetShell", new Color(0.12f, 0.14f, 0.16f), 0.5f, 0.85f); // Parlak siyah/antrasit kask kabuğu
                Material helmetVisorMat = GetMaterial("Mat_CourierHelmetVisor", new Color(0.06f, 0.10f, 0.15f), 0.9f, 0.98f); // Koyu yansımalı aerodinamik vizör
                Material helmetStripeMat = GetMaterial("Mat_CourierStripeNeon", new Color(0.18f, 0.85f, 0.35f), 0.2f, 0.5f); // Fosforlu yeşil yarış/güvenlik şeridi
                Material chinBarMat = GetMaterial("Mat_CourierChinBar", new Color(0.18f, 0.20f, 0.22f), 0.3f, 0.6f);

                // 1. Kask Ana Gövdesi (Kafayı saran aerodinamik kabuk)
                GameObject helmet = CreateBlock(parent, "Courier_Helmet", new Vector3(0f, 1.54f, 0.01f), new Vector3(0.38f, 0.36f, 0.38f), helmetShellMat);

                // 2. Koyu Yansımalı Ön Vizör (Göz hizasında geniş bombeli siperlik)
                CreateBlock(helmet.transform, "Visor", new Vector3(0f, 0.04f, 0.18f), new Vector3(0.32f, 0.14f, 0.06f), helmetVisorMat);

                // 3. Ön Çene Koruma Barı (Full-Face Chin Guard)
                CreateBlock(helmet.transform, "ChinGuard", new Vector3(0f, -0.10f, 0.16f), new Vector3(0.30f, 0.10f, 0.08f), chinBarMat);

                // 4. Kask Üstü Hava Menfezi & Aerodinamik Spoiler Tepesi
                CreateBlock(helmet.transform, "TopVent", new Vector3(0f, 0.18f, -0.04f), new Vector3(0.16f, 0.04f, 0.22f), chinBarMat);

                // 5. Kask Üzeri Fosforlu Yarış ve Güvenlik Şeritleri (Orta ve Yanlar)
                CreateBlock(helmet.transform, "Stripe_Top", new Vector3(0f, 0.19f, 0f), new Vector3(0.06f, 0.02f, 0.36f), helmetStripeMat);
                CreateBlock(helmet.transform, "Stripe_L", new Vector3(-0.19f, 0f, 0f), new Vector3(0.02f, 0.05f, 0.30f), helmetStripeMat);
                CreateBlock(helmet.transform, "Stripe_R", new Vector3(0.19f, 0f, 0f), new Vector3(0.02f, 0.05f, 0.30f), helmetStripeMat);

                return; // Kurye kask taktığı için saç oluşturulmaz
            }

            // Saç, ProceduralHumanFaceBuilder tarafından üretilir.
        }

        private static void BuildRoleUniformAccessories(Transform parent, StaffRole role, bool isFemale, Material skinMat)
        {
            Material goldBadge = GetMaterial("Mat_GoldBadge", new Color(0.95f, 0.80f, 0.15f), 0.9f, 0.9f);
            Material nameTagMat = GetMaterial("Mat_NameTag", Color.white);
            Material blackAcc = GetMaterial("Mat_AccBlack", new Color(0.12f, 0.12f, 0.14f));
            Material yellowGloves = GetMaterial("Mat_YellowGloves", new Color(0.95f, 0.85f, 0.10f));

            switch (role)
            {
                case StaffRole.Kasiyer:
                    // Yaka İsim Kartı & Kulaklık Mikrofon
                    CreateBlock(parent, "NameTag", new Vector3(0.12f, 1.22f, 0.16f), new Vector3(0.10f, 0.08f, 0.02f), nameTagMat);
                    GameObject headset = CreateBlock(parent, "Headset", new Vector3(0f, 1.66f, 0f), new Vector3(0.34f, 0.04f, 0.20f), blackAcc);
                    CreateBlock(headset.transform, "Mic", new Vector3(-0.16f, -0.10f, 0.12f), new Vector3(0.04f, 0.04f, 0.15f), blackAcc);
                    break;

                case StaffRole.Reyoncu:
                    // Ön Lojistik Önlük & Taşıma Kemer Bel Çantası
                    CreateBlock(parent, "Apron", new Vector3(0f, 1.05f, 0.16f), new Vector3(0.42f, 0.45f, 0.02f), GetMaterial("Mat_ApronGreen", new Color(0.18f, 0.65f, 0.35f)));
                    CreateBlock(parent, "ToolBelt", new Vector3(0f, 0.82f, 0f), new Vector3(0.54f, 0.08f, 0.32f), blackAcc);
                    break;

                case StaffRole.Temizlikçi:
                    // Temizlik Eldivenleri & Paspas Mop Tutucu
                    CreateBlock(parent, "Glove_L", new Vector3(-0.25f, 0.92f, 0f), new Vector3(0.16f, 0.18f, 0.17f), yellowGloves);
                    CreateBlock(parent, "Glove_R", new Vector3(0.25f, 0.92f, 0f), new Vector3(0.16f, 0.18f, 0.17f), yellowGloves);
                    CreateBlock(parent, "MopHandle", new Vector3(0.30f, 0.90f, 0.15f), new Vector3(0.05f, 1.20f, 0.05f), GetMaterial("Mat_MopStick", new Color(0.70f, 0.70f, 0.75f)));
                    break;

                case StaffRole.Güvenlik:
                    // Özel Güvenlik Şapkası, Göğüs Arması & Telsiz
                    CreateBlock(parent, "Sec_Cap", new Vector3(0f, 1.68f, 0.04f), new Vector3(0.36f, 0.10f, 0.38f), blackAcc);
                    CreateBlock(parent, "Sec_Badge", new Vector3(-0.12f, 1.22f, 0.16f), new Vector3(0.08f, 0.10f, 0.02f), goldBadge);
                    CreateBlock(parent, "WalkieTalkie", new Vector3(0.24f, 1.08f, 0.16f), new Vector3(0.08f, 0.16f, 0.06f), blackAcc);
                    break;

                case StaffRole.MüşteriHizmetlisi:
                    // Beyaz Takım Elbise / Beyaz Elbise Aksesuarları
                    Material redSilk = GetMaterial("Mat_RedSilkTie", new Color(0.85f, 0.20f, 0.20f));
                    CreateBlock(parent, "GoldBadge", new Vector3(-0.12f, 1.22f, 0.16f), new Vector3(0.08f, 0.10f, 0.02f), goldBadge);

                    if (isFemale)
                    {
                        // Kadın: Beyaz Elbise Şık Kırmızı Fular
                        CreateBlock(parent, "SilkScarf", new Vector3(0f, 1.34f, 0.15f), new Vector3(0.14f, 0.12f, 0.04f), redSilk);
                    }
                    else
                    {
                        // Erkek: Beyaz Takım Elbise Kırmızı Kravatı
                        CreateBlock(parent, "SuitTie", new Vector3(0f, 1.15f, 0.16f), new Vector3(0.06f, 0.30f, 0.02f), redSilk);
                    }

                    // Şık Danışma Kulaklığı & Mikrofonu
                    GameObject custHeadset = CreateBlock(parent, "HeadsetCS", new Vector3(0f, 1.66f, 0f), new Vector3(0.34f, 0.04f, 0.20f), blackAcc);
                    CreateBlock(custHeadset.transform, "MicCS", new Vector3(-0.16f, -0.10f, 0.12f), new Vector3(0.04f, 0.04f, 0.15f), blackAcc);
                    break;

                case StaffRole.Kurye:
                    // 🏍️ MOTORCU KIYAFETİ DETAYLARI: Deri Mont, Reflektör Şeritler, Koruma Pedleri, Eldivenler
                    Material bikerLeather = GetMaterial("Mat_BikerLeather", new Color(0.10f, 0.12f, 0.14f), 0.3f, 0.7f);
                    Material neonReflector = GetMaterial("Mat_NeonReflector", new Color(0.18f, 0.85f, 0.35f), 0.1f, 0.4f);
                    Material silverZip = GetMaterial("Mat_SilverZip", new Color(0.85f, 0.85f, 0.90f), 0.9f, 0.8f);
                    Material armorPad = GetMaterial("Mat_ArmorPad", new Color(0.16f, 0.18f, 0.20f), 0.4f, 0.6f);

                    // 1. Deri Mont Ön Fermuarı & Dik Yaka
                    CreateBlock(parent, "Jacket_Zipper", new Vector3(0f, 1.05f, 0.16f), new Vector3(0.04f, 0.50f, 0.02f), silverZip);
                    CreateBlock(parent, "Jacket_Collar", new Vector3(0f, 1.30f, 0.02f), new Vector3(0.46f, 0.06f, 0.30f), bikerLeather);

                    // 2. Göğüs ve Sırt Fosforlu Güvenlik/Teslimat Reflektör Şeritleri
                    CreateBlock(parent, "Reflector_Chest_L", new Vector3(-0.13f, 1.15f, 0.16f), new Vector3(0.05f, 0.28f, 0.02f), neonReflector);
                    CreateBlock(parent, "Reflector_Chest_R", new Vector3(0.13f, 1.15f, 0.16f), new Vector3(0.05f, 0.28f, 0.02f), neonReflector);
                    CreateBlock(parent, "Reflector_Back_Stripe", new Vector3(0f, 1.15f, -0.16f), new Vector3(0.36f, 0.08f, 0.02f), neonReflector);

                    // 3. Omuz & Dirsek Güçlendirilmiş Koruma Pedleri (Armor Pads)
                    CreateBlock(parent, "Shoulder_L", new Vector3(-0.25f, 1.28f, 0f), new Vector3(0.10f, 0.08f, 0.20f), armorPad);
                    CreateBlock(parent, "Shoulder_R", new Vector3(0.25f, 1.28f, 0f), new Vector3(0.10f, 0.08f, 0.20f), armorPad);

                    // 4. Dizlik Koruma Pedleri (Bacaklarda)
                    CreateBlock(parent, "KneePad_L", new Vector3(-0.14f, 0.52f, 0.11f), new Vector3(0.12f, 0.14f, 0.04f), armorPad);
                    CreateBlock(parent, "KneePad_R", new Vector3(0.14f, 0.52f, 0.11f), new Vector3(0.12f, 0.14f, 0.04f), armorPad);

                    // 5. Deri Motorcu Sürüş Eldivenleri (Ellerde)
                    CreateBlock(parent, "RidingGlove_L", new Vector3(-0.32f, 0.95f, 0f), new Vector3(0.14f, 0.16f, 0.15f), bikerLeather);
                    CreateBlock(parent, "RidingGlove_R", new Vector3(0.32f, 0.95f, 0f), new Vector3(0.14f, 0.16f, 0.15f), bikerLeather);

                    // 6. Bel Hızlı Teslimat & Pos Cihazı Çantası
                    CreateBlock(parent, "Courier_HipPouch", new Vector3(0.22f, 0.82f, 0.08f), new Vector3(0.12f, 0.15f, 0.12f), blackAcc);
                    break;

                case StaffRole.Çiftçi:
                case StaffRole.DeneyimliÇiftçi:
                case StaffRole.UstaÇiftlikSorumlusu:
                    // Çiftçi Hasır Şapkası
                    CreateBlock(parent, "Farmer_Hat", new Vector3(0f, 1.68f, 0f), new Vector3(0.55f, 0.08f, 0.55f), GetMaterial("Mat_HatStraw", new Color(0.90f, 0.80f, 0.25f)));
                    break;

                case StaffRole.TarımOtomasyonUzmanı:
                    // Mühendis Baret & Akıllı Tablet
                    CreateBlock(parent, "Eng_Helmet", new Vector3(0f, 1.68f, 0f), new Vector3(0.36f, 0.12f, 0.36f), GetMaterial("Mat_HelmetWhite", Color.white));
                    CreateBlock(parent, "Tablet", new Vector3(0.25f, 1.0f, 0.15f), new Vector3(0.04f, 0.25f, 0.32f), blackAcc);
                    break;
            }
        }

        private static GameObject CreateBlock(Transform parent, string name, Vector3 localPos, Vector3 localScale, Material mat)
        {
            GameObject cube = PrimitiveFactory.CreateVisualCube(name, parent);
            cube.transform.localPosition = localPos;
            cube.transform.localScale = localScale;

            if (mat != null)
            {
                cube.GetComponent<Renderer>().sharedMaterial = mat;
            }

            return cube;
        }

        private static GameObject CreateSeatedServiceLeg(Transform parent, string name, float x, Material pants, Material shoe)
        {
            GameObject hip = new GameObject(name);
            hip.transform.SetParent(parent, false);
            hip.transform.localPosition = new Vector3(x, 0.80f, 0f);
            CreateBlock(hip.transform, "Thigh", new Vector3(0f, -0.18f, 0f), new Vector3(0.18f, 0.36f, 0.20f), pants);
            Transform knee = new GameObject("ServiceKnee").transform;
            knee.SetParent(hip.transform, false);
            knee.localPosition = new Vector3(0f, -0.36f, 0f);
            CreateBlock(knee, "Shin", new Vector3(0f, -0.17f, 0f), new Vector3(0.17f, 0.34f, 0.18f), pants);
            CreateBlock(knee, "Shoe", new Vector3(0f, -0.37f, 0.05f), new Vector3(0.19f, 0.10f, 0.29f), shoe);
            return hip;
        }

        private static GameObject CreateLimb(Transform parent, string name, Vector3 localPos, Vector3 localScale, Material pantsMat, Material shoeMat)
        {
            GameObject pivot = new GameObject(name);
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = localPos + new Vector3(0f, localScale.y * 0.5f, 0f);

            GameObject legMesh = PrimitiveFactory.CreateVisualCube(name + "_Mesh", pivot.transform);
            legMesh.transform.localPosition = new Vector3(0f, -localScale.y * 0.5f, 0f);
            legMesh.transform.localScale = localScale;

            if (pantsMat != null) legMesh.GetComponent<Renderer>().sharedMaterial = pantsMat;

            GameObject shoe = PrimitiveFactory.CreateVisualCube("Shoe", legMesh.transform);
            shoe.transform.localPosition = new Vector3(0f, -0.45f, 0.12f);
            shoe.transform.localScale = new Vector3(1.05f, 0.18f, 1.3f);

            if (shoeMat != null) shoe.GetComponent<Renderer>().sharedMaterial = shoeMat;

            return pivot;
        }

        private static GameObject CreateArm(Transform parent, string name, Vector3 localPos, Vector3 localScale, Material sleeveMat, Material skinMat)
        {
            GameObject pivot = new GameObject(name);
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = localPos + new Vector3(0f, localScale.y * 0.5f, 0f);

            GameObject armMesh = PrimitiveFactory.CreateVisualCube(name + "_Mesh", pivot.transform);
            armMesh.transform.localPosition = new Vector3(0f, -localScale.y * 0.5f, 0f);
            armMesh.transform.localScale = localScale;

            if (sleeveMat != null) armMesh.GetComponent<Renderer>().sharedMaterial = sleeveMat;

            GameObject hand = PrimitiveFactory.CreateVisualCube("Hand", armMesh.transform);
            hand.transform.localPosition = new Vector3(0f, -0.48f, 0f);
            hand.transform.localScale = new Vector3(0.9f, 0.20f, 0.9f);

            if (skinMat != null) hand.GetComponent<Renderer>().sharedMaterial = skinMat;

            return pivot;
        }
    }
}
