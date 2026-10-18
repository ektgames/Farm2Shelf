using System.Collections.Generic;
using UnityEngine;
using Farm2Shelf.Core;

namespace Farm2Shelf.Environment
{
    // Stable IDs and a single source for buildings, delivery addresses and taxi stops.
    public static class GardenNeighborhoodBuilder
    {
        public sealed class Address
        {
            public string id, tr, en;
            public Vector3 center, doorstep;
            public bool apartment;
        }

        public static readonly Address[] Addresses = CreateAddresses();

        private static Address[] CreateAddresses()
        {
            var result = new List<Address>();
            float[] rows = { -35f, -57f, -79f, -105f };
            for (int col = 0; col < 2; col++)
            for (int row = 0; row < rows.Length; row++)
            {
                bool apartment = row == 3;
                int number = col * 3 + row + 1;
                result.Add(new Address
                {
                    id = "garden_neighborhood_" + col + "_" + row,
                    tr = apartment ? "Bahçevler Apartmanı " + (col + 1) : "Bahçevler Bahçeli Ev No:" + number,
                    en = apartment ? "Garden Quarter Apartments " + (col + 1) : "Garden Quarter House #" + number,
                    center = new Vector3(col == 0 ? 93.75f : 131.25f, 0, rows[row]),
                    doorstep = new Vector3(col == 0 ? 109f : 146.5f, .05f, rows[row]),
                    apartment = apartment
                });
            }
            return result.ToArray();
        }

        public static void Build(Transform parent)
        {
            if (parent.Find("Garden_Neighborhood") != null) return;
            var root = new GameObject("Garden_Neighborhood").transform;
            root.SetParent(parent, false);
            for (int i = 0; i < Addresses.Length; i++)
            {
                Address address = Addresses[i];
                GameObject parcel;
                if (address.apartment)
                {
                    parcel = ProceduralApartmentModelBuilder.BuildApartmentParcel(root, address.center,
                        new Vector2(31.5f, 22f), 3, i % 4, 100 + i, true, i < 4 ? 3.25f : 2f);
                }
                else
                {
                    // Build facing north, then rotate the complete garden/house to the east.
                    // The swapped parcel dimensions leave the existing sidewalks untouched.
                    parcel = ProceduralTownshipDistrictBuilder.BuildResidentialHouseParcel(root, address.center,
                        new Vector2(20f, 31.5f), i % 2 + 1, i % 8, 100 + i, true, i < 4 ? 3.25f : 2f);
                    parcel.transform.rotation = Quaternion.Euler(0, 90, 0);
                }
                parcel.name = address.id;
            }
        }

        public static TownContractPartner[] AppendContracts(TownContractPartner[] existing)
        {
            var result = new List<TownContractPartner>(existing);
            foreach (Address a in Addresses)
                result.Add(new TownContractPartner
                {
                    id = a.id, nameTr = a.tr, nameEn = a.en,
                    iconEmoji = a.apartment ? "🏢" : "🏡", doorstepPos = a.doorstep,
                    kind = TownContractKind.Residential, dailyPremium = a.apartment ? 95 : 85,
                    dailyQty = a.apartment ? 3 : 2,
                    preferredShelves = new[] { FurnitureType.Shelf, FurnitureType.Fridge, FurnitureType.ProduceShelf }
                });
            return result.ToArray();
        }

        public static (string nameTr, string nameEn, Vector3 doorstepPos)[] AppendDeliveries(
            (string nameTr, string nameEn, Vector3 doorstepPos)[] existing)
        {
            var result = new List<(string nameTr, string nameEn, Vector3 doorstepPos)>(existing);
            foreach (Address a in Addresses) result.Add((a.tr, a.en, a.doorstep));
            return result.ToArray();
        }
    }
}
