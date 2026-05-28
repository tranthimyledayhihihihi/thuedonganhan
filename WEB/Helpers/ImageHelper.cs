namespace WEB.Helpers
{
    public static class ImageHelper
    {
        // Unsplash Image URLs - Free to use, high quality
        public static class Products
        {
            // Laptops & Computers
            public const string MacbookAir = "https://images.unsplash.com/photo-1517694712202-14dd9538aa97?w=800&h=600&fit=crop";
            public const string LaptopDesk = "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?w=800&h=600&fit=crop";
            public const string DellLaptop = "https://images.unsplash.com/photo-1588872657578-7efd1f1555ed?w=800&h=600&fit=crop";
            
            // Cameras
            public const string SonyCamera = "https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=800&h=600&fit=crop";
            public const string CanonCamera = "https://images.unsplash.com/photo-1606980707146-b3a0c2d19e1f?w=800&h=600&fit=crop";
            public const string CameraLens = "https://images.unsplash.com/photo-1606800052052-a08af7148866?w=800&h=600&fit=crop";
            
            // Books & Study Materials
            public const string TextBooks = "https://images.unsplash.com/photo-1495446815901-a7297e633e8d?w=800&h=600&fit=crop";
            public const string StudyBooks = "https://images.unsplash.com/photo-1524995997946-a1c2e315a42f?w=800&h=600&fit=crop";
            public const string OpenBook = "https://images.unsplash.com/photo-1481627834876-b7833e8f5570?w=800&h=600&fit=crop";
            
            // Graduation
            public const string GraduationCap = "https://images.unsplash.com/photo-1523050854058-8df90110c9f1?w=800&h=600&fit=crop";
            public const string GraduationGown = "https://images.unsplash.com/photo-1627556704302-624c9b3f6833?w=800&h=600&fit=crop";
            
            // Electronics
            public const string Tablet = "https://images.unsplash.com/photo-1544244015-0df4b3ffc6b0?w=800&h=600&fit=crop";
            public const string Headphones = "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=800&h=600&fit=crop";
            public const string Keyboard = "https://images.unsplash.com/photo-1587829741301-dc798b83add3?w=800&h=600&fit=crop";
            
            // Stationery
            public const string Notebook = "https://images.unsplash.com/photo-1531346878377-a5be20888e57?w=800&h=600&fit=crop";
            public const string Pens = "https://images.unsplash.com/photo-1586075010923-2dd4570fb338?w=800&h=600&fit=crop";
            public const string Calculator = "https://images.unsplash.com/photo-1611224923853-80b023f02d71?w=800&h=600&fit=crop";
            
            // Bikes & Transportation
            public const string Bicycle = "https://images.unsplash.com/photo-1485965120184-e220f721d03e?w=800&h=600&fit=crop";
            public const string ElectricBike = "https://images.unsplash.com/photo-1559348349-86f1f65817fe?w=800&h=600&fit=crop";
            
            // Fashion
            public const string Backpack = "https://images.unsplash.com/photo-1553062407-98eeb64c6a62?w=800&h=600&fit=crop";
            public const string Watch = "https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=800&h=600&fit=crop";
        }

        public static class Categories
        {
            public const string Electronics = "https://images.unsplash.com/photo-1498049794561-7780e7231661?w=600&h=400&fit=crop";
            public const string Books = "https://images.unsplash.com/photo-1507842217343-583bb7270b66?w=600&h=400&fit=crop";
            public const string Fashion = "https://images.unsplash.com/photo-1445205170230-053b83016050?w=600&h=400&fit=crop";
            public const string Sports = "https://images.unsplash.com/photo-1461896836934-ffe607ba8211?w=600&h=400&fit=crop";
            public const string Music = "https://images.unsplash.com/photo-1511379938547-c1f69419868d?w=600&h=400&fit=crop";
            public const string Photography = "https://images.unsplash.com/photo-1452587925148-ce544e77e70d?w=600&h=400&fit=crop";
        }

        public static class Banners
        {
            public const string StudyDesk = "https://images.unsplash.com/photo-1434030216411-0b793f4b4173?w=1400&h=600&fit=crop";
            public const string Library = "https://images.unsplash.com/photo-1521737711867-e3b97375f902?w=1400&h=600&fit=crop";
            public const string Campus = "https://images.unsplash.com/photo-1562774053-701939374585?w=1400&h=600&fit=crop";
            public const string Graduation = "https://images.unsplash.com/photo-1541339907198-e08756dedf3f?w=1400&h=600&fit=crop";
            public const string StudentLife = "https://images.unsplash.com/photo-1523240795612-9a054b0db644?w=1400&h=600&fit=crop";
        }

        public static class Placeholders
        {
            public const string ProductDefault = "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=400&h=300&fit=crop";
            public const string UserAvatar = "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=200&h=200&fit=crop";
            public const string NoImage = "https://images.unsplash.com/photo-1533158326339-7f3cf2404354?w=400&h=300&fit=crop";
        }

        // Helper method to get optimized image URL
        public static string GetOptimizedUrl(string baseUrl, int width = 800, int height = 600, string fit = "crop")
        {
            if (string.IsNullOrEmpty(baseUrl))
                return Placeholders.NoImage;

            if (baseUrl.Contains("unsplash.com"))
            {
                var separator = baseUrl.Contains("?") ? "&" : "?";
                return $"{baseUrl}{separator}w={width}&h={height}&fit={fit}&q=80";
            }

            return baseUrl;
        }

        // Get random product image
        public static string GetRandomProductImage()
        {
            var images = new[]
            {
                Products.MacbookAir,
                Products.SonyCamera,
                Products.TextBooks,
                Products.GraduationCap,
                Products.Tablet,
                Products.Headphones,
                Products.Bicycle,
                Products.Backpack
            };

            var random = new Random();
            return images[random.Next(images.Length)];
        }
    }
}
