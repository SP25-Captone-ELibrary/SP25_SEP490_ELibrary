using FPTU_ELibrary.Domain.Entities;

namespace FPTU_ELibrary.Domain.Common.Enums;

public class LibraryLocation
{
    // [Floors]
    public static class Floors
    {
        /// <summary>
        /// First Floor
        ///     + 000-099: Computer science, information & general works
        ///     + 800-899: Literature
        ///     + 000-999: Children
        ///         > 000-099: General knowledge for children
        ///         > 100-199: Philosophy & Psychology for children (books on emotions, ethics)
        ///         > 200-299: Religion books for children (introducing world religion)
        ///         > 300-399: Social sciences for children (communication, society, life skills)
        ///         > 400-499: Language learning for children (English, foreign languages, alphabet books)
        ///         > 500-599: Natural sciences for children (animals, nature, simple experiments)
        ///         > 600-699: Technology & Engineering for children (robots, machines, basic medical knowledge)
        ///         > 700-799: Arts & Entertainment for children (drawing guides, music, crafts)
        ///         > 800-899: Children's literature (fairy tales, comics, children's novels)
        ///         > 900-999: History & Geography for children (simple history books, word exploration)
        /// </summary>
        public const int FirstFloor = 1;
        
        /// <summary>
        /// Second Floor
        ///     + 100-199: Philosophy & Psychology
        ///     + 300-399: Social sciences
        ///     + 400-499: Language
        ///     + 500-599: Natural sciences and mathematics
        ///     + 600-699: Technology
        ///     + 700-799: Arts & Recreation
        ///     + []    : Magazines & News
        ///     + []    : Reference
        /// </summary>
        public const int SecondFloor = 2;
        
        /// <summary>
        /// Third Floor
        ///     + 200-299: Religion
        ///     + 900-999: History & Geography
        /// </summary>
        public const int ThirdFloor = 3;
    }
    
    // [Zones]
    public static class Zones
    {
        // Lobby
        public const string Lobby = "Khu vực sảnh";
        // Checkout Counter
        public const string CheckoutCounter = "Quầy thủ thư";
        // Library stacks (book collection)
        public const string LibraryStacks = "Khu sách";
        // Reading space
        public const string ReadingSpace = "Khu đọc sách";
        // Meeting Room
        public const string MeetingRoom = "Phòng họp";
        // Rest Room
        public const string RestRoom = "Phòng nghỉ";
        // Computer zone
        public const string ComputerZone = "Khu máy tính";
        // Self-checkout station
        public const string SelfCheckoutStation = "Máy trả sách tự động";
        // Study area
        public const string StudyArea = "Khu vực học tập";
        // Auditorium
        public const string Auditorium = "Thính phòng";
        // Gallery
        public const string Gallery = "Phòng trưng bày";
        // Toilet
        public const string Toilet = "Nhà vệ sinh";
        // Trustees room
        public const string TrusteesRoom = "Phòng Hội đồng Quản Trị";
        // Admin/Development Office
        public const string AdminOffice = "Phòng Hành chính";
        // Printer
        public const string Printer = "Máy in";
    }
    
    // [Sections]
    public static class Sections
    {
        // 000-099: Computer science, information & general works (4 shelves)
        public static LibrarySection ComputerScienceAndGeneralWorks = new()
        {
            EngSectionName = "General Works, Computer Science & Information",
            VieSectionName = "Tổng hợp, Khoa học máy tính & Thông tin",
            ClassificationNumberRangeFrom = 000, // Total 3 digits, padding left '000'
            ClassificationNumberRangeTo = 099, // Total 3 digits, padding left '000'
            ShelfPrefix = "G",
            LibraryShelves = new List<LibraryShelf>()
            {
                // 000-019: General Knowledge
                new()
                {
                    ShelfNumber = "G-01",
                    EngShelfName = "General Knowledge",
                    VieShelfName = "Kiến thức chung & Bách khoa toàn thư",
                    ClassificationNumberRangeFrom = 000,
                    ClassificationNumberRangeTo = 019,
                    CreateDate = DateTime.Now
                },
                // 020 - 039: General Knowledge
                new()
                {
                    ShelfNumber = "G-02",
                    EngShelfName = "Library Science & Information Management",
                    VieShelfName = "Khoa học thư viện & Quản lý thông tin",
                    ClassificationNumberRangeFrom = 020,
                    ClassificationNumberRangeTo = 039,
                    CreateDate = DateTime.Now
                },
                // 060 - 079: General Knowledge
                new()
                {
                    ShelfNumber = "G-03",
                    EngShelfName = "Organizations, Associations & Journalism",
                    VieShelfName = "Tổ chức, Hiệp hội & Báo chí",
                    ClassificationNumberRangeFrom = 060,
                    ClassificationNumberRangeTo = 079,
                    CreateDate = DateTime.Now
                },
                // 080 - 099: Collected Works, Anthologies & Miscellaneous Writings
                new()
                {
                    ShelfNumber = "G-04",
                    EngShelfName = "Collected Works, Anthologies & Miscellaneous Writings",
                    VieShelfName = "Tác phẩm tổng hợp, Sưu tập văn bản",
                    ClassificationNumberRangeFrom = 080,
                    ClassificationNumberRangeTo = 099,
                    CreateDate = DateTime.Now
                }
            },
            CreateDate = DateTime.Now,
        };
        
        // 000-999: Children (6 shelves)
        public static LibrarySection Children = new()
        {
            EngSectionName = "Children",
            VieSectionName = "Sách Thiếu Nhi",
            ClassificationNumberRangeFrom = 000, // Total 3 digits, padding left '000'
            ClassificationNumberRangeTo = 999,
            ShelfPrefix = "C",
            IsChildrenSection = true,
            LibraryShelves = new List<LibraryShelf>()
            {
                new()
                {
                    ShelfNumber = "C-01",
                    EngShelfName = "Encyclopedias & General Knowledge",
                    VieShelfName = "Bách khoa & Kiến thức tổng hợp",
                    ClassificationNumberRangeFrom = 000,
                    ClassificationNumberRangeTo = 099,
                    CreateDate = DateTime.Now
                },
                new()
                {
                    ShelfNumber = "C-02",
                    EngShelfName = "Philosophy, Psychology & Religions",
                    VieShelfName = "Triết học, Tâm lý & Tôn giáo",
                    ClassificationNumberRangeFrom = 100,
                    ClassificationNumberRangeTo = 299,
                    CreateDate = DateTime.Now
                },
                new()
                {
                    ShelfNumber = "C-03",
                    EngShelfName = "Social Sciences & Languages",
                    VieShelfName = "Xã hội & Ngôn ngữ",
                    ClassificationNumberRangeFrom = 300,
                    ClassificationNumberRangeTo = 499,
                    CreateDate = DateTime.Now
                },
                new()
                {
                    ShelfNumber = "C-04",
                    EngShelfName = "Science, Technology & Engineering",
                    VieShelfName = "Khoa học, Công nghệ & Kỹ thuật",
                    ClassificationNumberRangeFrom = 500,
                    ClassificationNumberRangeTo = 699,
                    CreateDate = DateTime.Now
                },
                new()
                {
                    ShelfNumber = "C-05",
                    EngShelfName = "Arts, Recreation & Children's Literature",
                    VieShelfName = "Nghệ thuật, Giải trí & Văn học thiếu nhi",
                    ClassificationNumberRangeFrom = 700,
                    ClassificationNumberRangeTo = 899,
                    CreateDate = DateTime.Now
                },
                new()
                {
                    ShelfNumber = "C-06",
                    EngShelfName = "History & Geography",
                    VieShelfName = "Lịch sử & Địa lý",
                    ClassificationNumberRangeFrom = 900,
                    ClassificationNumberRangeTo = 999,
                    CreateDate = DateTime.Now
                }
            },
            CreateDate = DateTime.Now,
        };
        
        // 000-999: Reference (5 shelves)
        public static LibrarySection Reference = new()
        {
            EngSectionName = "Reference",
            VieSectionName = "Sách Tham Khảo",
            ClassificationNumberRangeFrom = 000, // Total 3 digits, padding left '000'
            ClassificationNumberRangeTo = 999,
            IsReferenceSection = true,
            ShelfPrefix = "RF",
            LibraryShelves = new List<LibraryShelf>()
            {
                new()
                {
                    ShelfNumber = "RF-01",
                    EngShelfName = "General & Philosophy",
                    VieShelfName = "Tổng hợp & Triết học",
                    ClassificationNumberRangeFrom = 000,
                    ClassificationNumberRangeTo = 199,
                    CreateDate = DateTime.Now
                },
                new()
                {
                    ShelfNumber = "RF-02",
                    EngShelfName = "Religion & Social Sciences",
                    VieShelfName = "Tôn giáo & Khoa học xã hội",
                    ClassificationNumberRangeFrom = 200,
                    ClassificationNumberRangeTo = 399,
                    CreateDate = DateTime.Now
                },
                new()
                {
                    ShelfNumber = "RF-03",
                    EngShelfName = "Language & Reference Tools",
                    VieShelfName = "Ngôn ngữ & Công cụ tham khảo",
                    ClassificationNumberRangeFrom = 400,
                    ClassificationNumberRangeTo = 499,
                    CreateDate = DateTime.Now
                },
                new()
                {
                    ShelfNumber = "RF-04",
                    EngShelfName = "Science & Technology",
                    VieShelfName = "Khoa học & Công nghệ",
                    ClassificationNumberRangeFrom = 500,
                    ClassificationNumberRangeTo = 699,
                    CreateDate = DateTime.Now
                },
                new()
                {
                    ShelfNumber = "RF-05",
                    EngShelfName = "Arts, Literature, History & Geography",
                    VieShelfName = "Nghệ thuật, Văn học, Lịch sử & Địa lý",
                    ClassificationNumberRangeFrom = 700,
                    ClassificationNumberRangeTo = 999,
                    CreateDate = DateTime.Now
                },
            },
            CreateDate = DateTime.Now,
        };
        
        // Magazines & News (5 shelves)
        public static LibrarySection MagazinesAndNews = new()
        {
            EngSectionName = "Magazines & News",
            VieSectionName = "Báo chí & Tạp chí",
            ClassificationNumberRangeFrom = 000,
            ClassificationNumberRangeTo = 999,
            ShelfPrefix = "M",
            IsJournalSection = true,
            LibraryShelves = new List<LibraryShelf>()
            {
                new()
                {
                    ShelfNumber = "M-01",
                    EngShelfName = "Newspapers Archive",
                    VieShelfName = "Lưu trữ Báo chí",
                    ClassificationNumberRangeFrom = 000,
                    ClassificationNumberRangeTo = 199,
                    CreateDate = DateTime.Now
                },
                new()
                {
                    ShelfNumber = "M-02",
                    EngShelfName = "Current Affairs & Politics",
                    VieShelfName = "Thời sự & Chính trị",
                    ClassificationNumberRangeFrom = 200,
                    ClassificationNumberRangeTo = 399,
                    CreateDate = DateTime.Now
                },
                new()
                {
                    ShelfNumber = "M-03",
                    EngShelfName = "Science & Technology",
                    VieShelfName = "Khoa học & Công nghệ",
                    ClassificationNumberRangeFrom = 400,
                    ClassificationNumberRangeTo = 599,
                    CreateDate = DateTime.Now
                },
                new()
                {
                    ShelfNumber = "M-04",
                    EngShelfName = "Lifestyle & Culture",
                    VieShelfName = "Phong cách sống & Văn hóa",
                    ClassificationNumberRangeFrom = 600,
                    ClassificationNumberRangeTo = 799,
                    CreateDate = DateTime.Now
                },
                new()
                {
                    ShelfNumber = "M-05",
                    EngShelfName = "Entertainment & Sports",
                    VieShelfName = "Giải trí & Thể thao",
                    ClassificationNumberRangeFrom = 800,
                    ClassificationNumberRangeTo = 999,
                    CreateDate = DateTime.Now
                },
            },
            CreateDate = DateTime.Now,
        };
        
        // 100-199: Philosophy & Psychology (5 shelves)
        public static LibrarySection PhilosophyAndPsychology = new()
        {
            EngSectionName = "Philosophy & Psychology",
            VieSectionName = "Triết học & Tâm lý học",
            ClassificationNumberRangeFrom = 100,
            ClassificationNumberRangeTo = 199,
            ShelfPrefix = "P",
            LibraryShelves = new List<LibraryShelf>()
            {
                // 100-119: General Philosophy
                new()
                {
                    ShelfNumber = "P-01",
                    EngShelfName = "General Philosophy",
                    VieShelfName = "Lý thuyết triết học tổng quát",
                    ClassificationNumberRangeFrom = 100,
                    ClassificationNumberRangeTo = 119,
                    CreateDate = DateTime.Now
                },
                // 120-139: Epistemology, Metaphysics & Analytical Philosophy
                new()
                {
                    ShelfNumber = "P-02",
                    EngShelfName = "Epistemology, Metaphysics & Analytical Philosophy",
                    VieShelfName = "Nhận thức luận, Siêu hình học & Triết học phân tích",
                    ClassificationNumberRangeFrom = 120,
                    ClassificationNumberRangeTo = 139,
                    CreateDate = DateTime.Now
                },
                // 140-159: Western & Modern Philosophy
                new()
                {
                    ShelfNumber = "P-03",
                    EngShelfName = "Western & Modern Philosophy",
                    VieShelfName = "Triết học phương Tây & Triết học hiện đại",
                    ClassificationNumberRangeFrom = 140,
                    ClassificationNumberRangeTo = 159,
                    CreateDate = DateTime.Now
                },
                // 160-179: Logic & Ethics
                new()
                {
                    ShelfNumber = "P-04",
                    EngShelfName = "Logic & Ethics",
                    VieShelfName = "Logic học & Đạo đức học",
                    ClassificationNumberRangeFrom = 160,
                    ClassificationNumberRangeTo = 179,
                    CreateDate = DateTime.Now
                },
                // 180-199: Eastern & Ancient Philosophy
                new()
                {
                    ShelfNumber = "P-05",
                    EngShelfName = "Eastern & Ancient Philosophy",
                    VieShelfName = "Triết học phương Đông & Triết học cổ đại",
                    ClassificationNumberRangeFrom = 180,
                    ClassificationNumberRangeTo = 199,
                    CreateDate = DateTime.Now
                }
            },
            CreateDate = DateTime.Now,
        };
        
        // 200-299: Religion (5 shelves)
        public static LibrarySection Religion = new()
        {
            EngSectionName = "Religion",
            VieSectionName = "Tôn Giáo",
            ClassificationNumberRangeFrom = 200,
            ClassificationNumberRangeTo = 299,
            ShelfPrefix = "R",
            LibraryShelves = new List<LibraryShelf>()
            {
                // 200-219: General Religion
                new()
                {
                    ShelfNumber = "R-01",
                    EngShelfName = "General Religion",
                    VieShelfName = "Tổng quan về Tôn giáo",
                    ClassificationNumberRangeFrom = 200,
                    ClassificationNumberRangeTo = 219,
                    CreateDate = DateTime.Now
                },
                // 220-239: Bible Studies & Scriptures
                new()
                {
                    ShelfNumber = "R-02",
                    EngShelfName = "Bible Studies & Scriptures",
                    VieShelfName = "Kinh Thánh & Nghiên cứu Kinh Thánh",
                    ClassificationNumberRangeFrom = 220,
                    ClassificationNumberRangeTo = 239,
                    CreateDate = DateTime.Now
                },
                // 240-259: Bible Studies & Scriptures
                new()
                {
                    ShelfNumber = "R-03",
                    EngShelfName = "Theology & Religious Practices",
                    VieShelfName = "Thần học & Thực hành tôn giáo",
                    ClassificationNumberRangeFrom = 240,
                    ClassificationNumberRangeTo = 259,
                    CreateDate = DateTime.Now
                },
                // 260-279: Church History
                new()
                {
                    ShelfNumber = "R-04",
                    EngShelfName = "Church History",
                    VieShelfName = "Giáo hội & Lịch sử nhà thờ",
                    ClassificationNumberRangeFrom = 260,
                    ClassificationNumberRangeTo = 279,
                    CreateDate = DateTime.Now
                },
                // 280-299: Other Religions (Buddhism, Islam, Hinduism, etc.)
                new()
                {
                    ShelfNumber = "R-05",
                    EngShelfName = "Other Religions (Buddhism, Islam, Hinduism, etc.)",
                    VieShelfName = "Các tôn giáo khác (Phật giáo, Hồi giáo, Hindu, v.v)",
                    ClassificationNumberRangeFrom = 280,
                    ClassificationNumberRangeTo = 299,
                    CreateDate = DateTime.Now
                },
            },
            CreateDate = DateTime.Now,
        };
        
        // 300-399: Social Sciences (12 shelves)
        public static LibrarySection SocialSciences = new()
        {
            EngSectionName = "Social Sciences",
            VieSectionName = "Khoa Học Xã Hội",
            ClassificationNumberRangeFrom = 300,
            ClassificationNumberRangeTo = 399,
            ShelfPrefix = "S",
            LibraryShelves = new List<LibraryShelf>()
            {
                // 300-309: Sociology & Anthropology
                new()
                {
                    ShelfNumber = "S-01",
                    EngShelfName = "Sociology & Anthropology",
                    VieShelfName = "Xã hội học & Nhân học",
                    ClassificationNumberRangeFrom = 300,
                    ClassificationNumberRangeTo = 309,
                    CreateDate = DateTime.Now
                },
                // 310-319: Statistics & Research Methods
                new()
                {
                    ShelfNumber = "S-02",
                    EngShelfName = "Statistics & Research Methods",
                    VieShelfName = "Thống kê & Phương pháp nghiên cứu",
                    ClassificationNumberRangeFrom = 310,
                    ClassificationNumberRangeTo = 319,
                    CreateDate = DateTime.Now
                },
                // 320-324: Political Science - Theory & Ideologies
                new()
                {
                    ShelfNumber = "S-03A",
                    EngShelfName = "Political Science - Theory & Ideologies",
                    VieShelfName = "Chính trị học - Lý thuyết & Tư tưởng",
                    ClassificationNumberRangeFrom = 320,
                    ClassificationNumberRangeTo = 324,
                    CreateDate = DateTime.Now
                },
                // 325-329: Political Science - Institutions & Government
                new()
                {
                    ShelfNumber = "S-03B",
                    EngShelfName = "Political Science - Institutions & Government",
                    VieShelfName = "Chính trị học - Thể chế & Chính quyền",
                    ClassificationNumberRangeFrom = 325,
                    ClassificationNumberRangeTo = 329,
                    CreateDate = DateTime.Now
                },
                // 330-334: Economics - Microeconomics & Theory
                new()
                {
                    ShelfNumber = "S-04A",
                    EngShelfName = "Economics - Microeconomics & Theory",
                    VieShelfName = "Kinh tế học - Kinh tế vi mô & Lý thuyết",
                    ClassificationNumberRangeFrom = 330,
                    ClassificationNumberRangeTo = 334,
                    CreateDate = DateTime.Now
                },
                // 335-339: Economics - Macroeconomics & Policy
                new()
                {
                    ShelfNumber = "S-04B",
                    EngShelfName = "Economics - Macroeconomics & Policy",
                    VieShelfName = "Kinh tế học - Kinh tế vĩ mô & Chính sách",
                    ClassificationNumberRangeFrom = 335,
                    ClassificationNumberRangeTo = 339,
                    CreateDate = DateTime.Now
                },
                // 340-349: Law
                new()
                {
                    ShelfNumber = "S-05",
                    EngShelfName = "Law",
                    VieShelfName = "Luật học",
                    ClassificationNumberRangeFrom = 340,
                    ClassificationNumberRangeTo = 349,
                    CreateDate = DateTime.Now
                },
                // 350-359: Public Administration & Military Science
                new()
                {
                    ShelfNumber = "S-06",
                    EngShelfName = "Public Administration & Military Science",
                    VieShelfName = "Hành chính công & Khoa học quân sự",
                    ClassificationNumberRangeFrom = 350,
                    ClassificationNumberRangeTo = 359,
                    CreateDate = DateTime.Now
                },
                // 360-369: Social Problems & Social Services
                new()
                {
                    ShelfNumber = "S-07",
                    EngShelfName = "Social Problems & Social Services",
                    VieShelfName = "Vấn đề xã hội & Dịch vụ xã hội",
                    ClassificationNumberRangeFrom = 360,
                    ClassificationNumberRangeTo = 369,
                    CreateDate = DateTime.Now
                },
                // 370-379: Education
                new()
                {
                    ShelfNumber = "S-08",
                    EngShelfName = "Education",
                    VieShelfName = "Giáo dục",
                    ClassificationNumberRangeFrom = 370,
                    ClassificationNumberRangeTo = 379,
                    CreateDate = DateTime.Now
                },
                // 380-389: Commerce, Communications & Transportation
                new()
                {
                    ShelfNumber = "S-09",
                    EngShelfName = "Commerce, Communications & Transportation",
                    VieShelfName = "Thương mại, Truyền thông & Giao thông",
                    ClassificationNumberRangeFrom = 380,
                    ClassificationNumberRangeTo = 389,
                    CreateDate = DateTime.Now
                },
                // 390-399: Customs, Etiquette & Folklore
                new()
                {
                    ShelfNumber = "S-10",
                    EngShelfName = "Customs, Etiquette & Folklore",
                    VieShelfName = "Phong tục, Nghi thức & Dân gian",
                    ClassificationNumberRangeFrom = 390,
                    ClassificationNumberRangeTo = 399,
                    CreateDate = DateTime.Now
                },
            },
            CreateDate = DateTime.Now,
        };

        // 400-499: Language (5 shelves)
        public static LibrarySection Language = new()
        {
            EngSectionName = "Language",
            VieSectionName = "Ngôn Ngữ",
            ClassificationNumberRangeFrom = 400,
            ClassificationNumberRangeTo = 499,
            ShelfPrefix = "L",
            LibraryShelves = new List<LibraryShelf>()
            {
                new()
                {
                    ShelfNumber = "L-01",
                    EngShelfName = "General Linguistics",
                    VieShelfName = "Ngôn ngữ học tổng quát",
                    ClassificationNumberRangeFrom = 400,
                    ClassificationNumberRangeTo = 419,
                    CreateDate = DateTime.Now
                },
                new()
                {
                    ShelfNumber = "L-02",
                    EngShelfName = "English & Germanic Languages",
                    VieShelfName = "Tiếng Anh & Các ngôn ngữ Đức",
                    ClassificationNumberRangeFrom = 420,
                    ClassificationNumberRangeTo = 439,
                    CreateDate = DateTime.Now
                },
                new()
                {
                    ShelfNumber = "L-03",
                    EngShelfName = "French & Romance Languages",
                    VieShelfName = "Tiếng Pháp & Các ngôn ngữ Rôman",
                    ClassificationNumberRangeFrom = 440,
                    ClassificationNumberRangeTo = 459,
                    CreateDate = DateTime.Now
                },
                new()
                {
                    ShelfNumber = "L-04",
                    EngShelfName = "Spanish, Portuguese & Italian",
                    VieShelfName = "Tiếng Tây Ban Nha, Bồ Đào Nha & Ý",
                    ClassificationNumberRangeFrom = 460,
                    ClassificationNumberRangeTo = 479,
                    CreateDate = DateTime.Now
                },
                new()
                {
                    ShelfNumber = "L-05",
                    EngShelfName = "Greek, Russian & Other Languages",
                    VieShelfName = "Các ngôn ngữ Hy Lạp, Nga, Đông Á & Các ngôn ngữ khác",
                    ClassificationNumberRangeFrom = 480,
                    ClassificationNumberRangeTo = 499,
                    CreateDate = DateTime.Now
                },
            },
            CreateDate = DateTime.Now,
        };

        // 500-599: Natural Science and Mathematics (12 shelves)
        public static LibrarySection NaturalSciencesAndMathematics = new()
        {
            EngSectionName = "Natural Sciences & Mathematics",
            VieSectionName = "Khoa Học Tự Nhiên",
            ClassificationNumberRangeFrom = 500,
            ClassificationNumberRangeTo = 599,
            ShelfPrefix = "N",
            LibraryShelves = new List<LibraryShelf>()
            {
                // 500-504: Pure Mathematics
                new()
                {
                    ShelfNumber = "N-01A",
                    EngShelfName = "Pure Mathematics",
                    VieShelfName = "Toán học thuần túy",
                    ClassificationNumberRangeFrom = 500,
                    ClassificationNumberRangeTo = 504,
                    CreateDate = DateTime.Now
                },
                // 505-509: Applied Mathematics
                new()
                {
                    ShelfNumber = "N-01B",
                    EngShelfName = "Applied Mathematics",
                    VieShelfName = "Toán học ứng dụng",
                    ClassificationNumberRangeFrom = 505,
                    ClassificationNumberRangeTo = 509,
                    CreateDate = DateTime.Now
                },
                // 510-519: Statistics
                new()
                {
                    ShelfNumber = "N-01C",
                    EngShelfName = "Statistics",
                    VieShelfName = "Thống kê",
                    ClassificationNumberRangeFrom = 510,
                    ClassificationNumberRangeTo = 519,
                    CreateDate = DateTime.Now
                },
                // 520-529: Astronomy
                new()
                {
                    ShelfNumber = "N-02A",
                    EngShelfName = "Astronomy",
                    VieShelfName = "Thiên văn học",
                    ClassificationNumberRangeFrom = 520,
                    ClassificationNumberRangeTo = 529,
                    CreateDate = DateTime.Now
                },
                // 530-539: Physics
                new()
                {
                    ShelfNumber = "N-02B",
                    EngShelfName = "Physics",
                    VieShelfName = "Vật lý",
                    ClassificationNumberRangeFrom = 530,
                    ClassificationNumberRangeTo = 539,
                    CreateDate = DateTime.Now
                },
                // 540-549: Chemistry
                new()
                {
                    ShelfNumber = "N-03A",
                    EngShelfName = "Chemistry",
                    VieShelfName = "Hóa học",
                    ClassificationNumberRangeFrom = 540,
                    ClassificationNumberRangeTo = 549,
                    CreateDate = DateTime.Now
                },
                // 550-559: Materials Science
                new()
                {
                    ShelfNumber = "N-03B",
                    EngShelfName = "Materials Science",
                    VieShelfName = "Khoa học vật liệu",
                    ClassificationNumberRangeFrom = 550,
                    ClassificationNumberRangeTo = 559,
                    CreateDate = DateTime.Now
                },
                // 560-565: General Biology
                new()
                {
                    ShelfNumber = "N-04A",
                    EngShelfName = "General Biology",
                    VieShelfName = "Sinh học tổng hợp",
                    ClassificationNumberRangeFrom = 560,
                    ClassificationNumberRangeTo = 565,
                    CreateDate = DateTime.Now
                },
                // 566-569: Specialized Biology
                new()
                {
                    ShelfNumber = "N-04B",
                    EngShelfName = "Specialized Biology",
                    VieShelfName = "Sinh học chuyên sâu",
                    ClassificationNumberRangeFrom = 566,
                    ClassificationNumberRangeTo = 569,
                    CreateDate = DateTime.Now
                },
                // 570-579: Geology
                new()
                {
                    ShelfNumber = "N-04C",
                    EngShelfName = "Geology",
                    VieShelfName = "Địa chất học",
                    ClassificationNumberRangeFrom = 570,
                    ClassificationNumberRangeTo = 579,
                    CreateDate = DateTime.Now
                },
                // 580-589: Botany
                new()
                {
                    ShelfNumber = "N-05A",
                    EngShelfName = "Botany",
                    VieShelfName = "Thực vật học",
                    ClassificationNumberRangeFrom = 580,
                    ClassificationNumberRangeTo = 589,
                    CreateDate = DateTime.Now
                },
                // 590-599: Zoology
                new()
                {
                    ShelfNumber = "N-05B",
                    EngShelfName = "Zoology",
                    VieShelfName = "Động vật học",
                    ClassificationNumberRangeFrom = 590,
                    ClassificationNumberRangeTo = 599,
                    CreateDate = DateTime.Now
                },
            },
            CreateDate = DateTime.Now,
        };

        // 600-699: Technology (5 shelves)
        public static LibrarySection Technology = new()
        {
            EngSectionName = "Technology",
            VieSectionName = "Công Nghệ & Kỹ Thuật",
            ClassificationNumberRangeFrom = 600,
            ClassificationNumberRangeTo = 699,
            ShelfPrefix = "T",
            LibraryShelves = new List<LibraryShelf>()
            {
                // 680-699: Computing, Information & Emerging Technologies
                new()
                {
                    ShelfNumber = "T-01",
                    EngShelfName = "Computing, Information & Emerging Technologies",
                    VieShelfName = "Tin học, Công nghệ thông tin & Công nghệ mới",
                    ClassificationNumberRangeFrom = 680,
                    ClassificationNumberRangeTo = 699,
                    CreateDate = DateTime.Now
                },
                // 600-619: General Engineering & Applied Sciences
                new()
                {
                    ShelfNumber = "T-02",
                    EngShelfName = "General Engineering & Applied Sciences",
                    VieShelfName = "Kỹ thuật tổng quát & Ứng dụng",
                    ClassificationNumberRangeFrom = 600,
                    ClassificationNumberRangeTo = 619,
                    CreateDate = DateTime.Now
                },
                // 620-639: Mechanical & Civil Engineering
                new()
                {
                    ShelfNumber = "T-03",
                    EngShelfName = "Mechanical & Civil Engineering",
                    VieShelfName = "Kỹ thuật cơ khí & Dân dụng",
                    ClassificationNumberRangeFrom = 620,
                    ClassificationNumberRangeTo = 639,
                    CreateDate = DateTime.Now
                },
                // 640-659: Electrical & Electronics Engineering
                new()
                {
                    ShelfNumber = "T-04",
                    EngShelfName = "Electrical & Electronics Engineering",
                    VieShelfName = "Kỹ thuật điện & Điện tử",
                    ClassificationNumberRangeFrom = 640,
                    ClassificationNumberRangeTo = 659,
                    CreateDate = DateTime.Now
                },
                // 660-679: Chemical & Materials Engineering
                new()
                {
                    ShelfNumber = "T-05",
                    EngShelfName = "Chemical & Materials Engineering",
                    VieShelfName = "Kỹ thuật hóa học & Vật liệu",
                    ClassificationNumberRangeFrom = 660,
                    ClassificationNumberRangeTo = 679,
                    CreateDate = DateTime.Now
                }
            },
            CreateDate = DateTime.Now,
        };
        
        // 700-799: Arts & Recreation (5 shelves)
        public static LibrarySection ArtsAndRecreation = new()
        {
            EngSectionName = "Arts & Recreation",
            VieSectionName = "Nghệ thuật & Giải trí",
            ClassificationNumberRangeFrom = 700,
            ClassificationNumberRangeTo = 799,
            ShelfPrefix = "A",
            LibraryShelves = new List<LibraryShelf>()
            {
                new()
                {
                    ShelfNumber = "A-01",
                    EngShelfName = "Art Theory & Design",
                    VieShelfName = "Lý thuyết nghệ thuật & Thiết kế",
                    ClassificationNumberRangeFrom = 700,
                    ClassificationNumberRangeTo = 719,
                    CreateDate = DateTime.Now
                },
                new()
                {
                    ShelfNumber = "A-02",
                    EngShelfName = "Architecture & Sculpture",
                    VieShelfName = "Kiến trúc & Điêu khắc",
                    ClassificationNumberRangeFrom = 720,
                    ClassificationNumberRangeTo = 739,
                    CreateDate = DateTime.Now
                },
                new()
                {
                    ShelfNumber = "A-03",
                    EngShelfName = "Painting & Illustration",
                    VieShelfName = "Hội họa & Minh họa",
                    ClassificationNumberRangeFrom = 740,
                    ClassificationNumberRangeTo = 759,
                    CreateDate = DateTime.Now
                },
                new()
                {
                    ShelfNumber = "A-04",
                    EngShelfName = "Photography & Printmaking",
                    VieShelfName = "Nhiếp ảnh & Nghệ thuật in ấn",
                    ClassificationNumberRangeFrom = 760,
                    ClassificationNumberRangeTo = 779,
                    CreateDate = DateTime.Now
                },
                new()
                {
                    ShelfNumber = "A-05",
                    EngShelfName = "Music & Entertainment",
                    VieShelfName = "Âm nhạc & Giải trí",
                    ClassificationNumberRangeFrom = 780,
                    ClassificationNumberRangeTo = 799,
                    CreateDate = DateTime.Now
                }
            },
            CreateDate = DateTime.Now,
        };

        // 800-899: Literature (12 shelves)
        public static LibrarySection Literature = new()
        {
            EngSectionName = "Literature",
            VieSectionName = "Văn Học",
            ClassificationNumberRangeFrom = 800,
            ClassificationNumberRangeTo = 899,
            ShelfPrefix = "L",
            LibraryShelves = new List<LibraryShelf>()
            {
                new()
                {
                    ShelfNumber = "L-01",
                    EngShelfName = "Literary Theory & Criticism",
                    VieShelfName = "Lý thuyết văn học & Phê bình",
                    ClassificationNumberRangeFrom = 800,
                    ClassificationNumberRangeTo = 819,
                    CreateDate = DateTime.Now
                },
                new()
                {
                    ShelfNumber = "L-02A",
                    EngShelfName = "English Literature (Fiction)",
                    VieShelfName = "Văn học Anh (Tiếu thuyết)",
                    ClassificationNumberRangeFrom = 820,
                    ClassificationNumberRangeTo = 829,
                    CreateDate = DateTime.Now
                },
                new()
                {
                    ShelfNumber = "L-02B",
                    EngShelfName = "English Literature (Poetry & Drama)",
                    VieShelfName = "Văn học Anh (Thơ & Kịch)",
                    ClassificationNumberRangeFrom = 820,
                    ClassificationNumberRangeTo = 829,
                    CreateDate = DateTime.Now
                },
                new()
                {
                    ShelfNumber = "L-03A",
                    EngShelfName = "German Literature (Fiction)",
                    VieShelfName = "Văn học Đức (Tiếu thuyết)",
                    ClassificationNumberRangeFrom = 830,
                    ClassificationNumberRangeTo = 839,
                    CreateDate = DateTime.Now
                },
                new()
                {
                    ShelfNumber = "L-03B",
                    EngShelfName = "German Literature (Poetry & Drama)",
                    VieShelfName = "Văn học Đức (Thơ & Kịch)",
                    ClassificationNumberRangeFrom = 830,
                    ClassificationNumberRangeTo = 839,
                    CreateDate = DateTime.Now
                },
                new()
                {
                    ShelfNumber = "L-04A",
                    EngShelfName = "French Literature (Classics)",
                    VieShelfName = "Văn học Pháp (Kinh điển)",
                    ClassificationNumberRangeFrom = 840,
                    ClassificationNumberRangeTo = 849,
                    CreateDate = DateTime.Now
                },
                new()
                {
                    ShelfNumber = "L-04B",
                    EngShelfName = "French Literature (Modern)",
                    VieShelfName = "Văn học Pháp (Hiện đại)",
                    ClassificationNumberRangeFrom = 840,
                    ClassificationNumberRangeTo = 849,
                    CreateDate = DateTime.Now
                },
                new()
                {
                    ShelfNumber = "L-05A",
                    EngShelfName = "Spanish Literature",
                    VieShelfName = "Văn học Tây Ban Nha",
                    ClassificationNumberRangeFrom = 850,
                    ClassificationNumberRangeTo = 859,
                    CreateDate = DateTime.Now
                },
                new()
                {
                    ShelfNumber = "L-05B",
                    EngShelfName = "Spanish Literature (Latin America)",
                    VieShelfName = "Văn học Tây Ban Nha (Mỹ Latin)",
                    ClassificationNumberRangeFrom = 850,
                    ClassificationNumberRangeTo = 859,
                    CreateDate = DateTime.Now
                },
                new()
                {
                    ShelfNumber = "L-06",
                    EngShelfName = "Italian Literature",
                    VieShelfName = "Văn học Ý",
                    ClassificationNumberRangeFrom = 860,
                    ClassificationNumberRangeTo = 869,
                    CreateDate = DateTime.Now
                },
                new()
                {
                    ShelfNumber = "L-07",
                    EngShelfName = "Latin American Literature (Non-Spanish)",
                    VieShelfName = "Văn học Mỹ Latin (không phải tiếng Tây Ban Nha)",
                    ClassificationNumberRangeFrom = 870,
                    ClassificationNumberRangeTo = 879,
                    CreateDate = DateTime.Now
                },
                new()
                {
                    ShelfNumber = "L-08",
                    EngShelfName = "Greek, Russian & Other World Literature",
                    VieShelfName = "Văn học Hy Lạp, Nga & Các nền văn học khác",
                    ClassificationNumberRangeFrom = 880,
                    ClassificationNumberRangeTo = 899,
                    CreateDate = DateTime.Now
                }
            },
            CreateDate = DateTime.Now,
        };

        // 900-999: History & Geography (5 shelves)
        public static LibrarySection HistoryAndGeography = new()
        {
            EngSectionName = "History & Geography",
            VieSectionName = "Lịch Sử & Địa Lý",
            ClassificationNumberRangeFrom = 900,
            ClassificationNumberRangeTo = 999,
            ShelfPrefix = "H",
            LibraryShelves = new List<LibraryShelf>()
            {
                new()
                {
                    ShelfNumber = "H-01",
                    EngShelfName = "General History & Geography",
                    VieShelfName = "Lịch sử & Địa lý tổng quát",
                    ClassificationNumberRangeFrom = 900,
                    ClassificationNumberRangeTo = 919,
                    CreateDate = DateTime.Now,
                },
                new()
                {
                    ShelfNumber = "H-02",
                    EngShelfName = "Biographies & Historical Figures",
                    VieShelfName = "Tiểu sử & Nhân vật lịch sử",
                    ClassificationNumberRangeFrom = 920,
                    ClassificationNumberRangeTo = 939,
                    CreateDate = DateTime.Now,
                },
                new()
                {
                    ShelfNumber = "H-03",
                    EngShelfName = "European & Asian History",
                    VieShelfName = "Lịch sử châu Âu & châu Á",
                    ClassificationNumberRangeFrom = 940,
                    ClassificationNumberRangeTo = 959,
                    CreateDate = DateTime.Now,
                },
                new()
                {
                    ShelfNumber = "H-04",
                    EngShelfName = "African & American History",
                    VieShelfName = "Lịch sử châu Phi & châu Mỹ",
                    ClassificationNumberRangeFrom = 960,
                    ClassificationNumberRangeTo = 979,
                    CreateDate = DateTime.Now,
                },
                new()
                {
                    ShelfNumber = "H-05",
                    EngShelfName = "History of Australia, New Zealand & Polar Regions",
                    VieShelfName = "Lịch sử Úc, New Zealand & Vùng cực",
                    ClassificationNumberRangeFrom = 980,
                    ClassificationNumberRangeTo = 999,
                    CreateDate = DateTime.Now,
                }
            },
            CreateDate = DateTime.Now,
        };
    }
}