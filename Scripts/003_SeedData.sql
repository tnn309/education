-- Thêm dữ liệu mẫu
INSERT INTO "AspNetRoles" ("Id", "Name", "NormalizedName", "ConcurrencyStamp")
VALUES 
    ('1', 'Admin', 'ADMIN', gen_random_uuid()::text),
    ('2', 'Parent', 'PARENT', gen_random_uuid()::text),
    ('3', 'Student', 'STUDENT', gen_random_uuid()::text);

-- Thêm giáo viên mẫu
INSERT INTO "Teachers" ("FullName", "Email", "PhoneNumber", "Specialization", "Experience", "Bio")
VALUES 
    ('Nguyễn Thị Lan', 'lan.nguyen@center.com', '0901234567', 'Kỹ năng sống', 5, 'Giáo viên có 5 năm kinh nghiệm dạy kỹ năng sống cho trẻ em'),
    ('Trần Văn Minh', 'minh.tran@center.com', '0901234568', 'Thể thao', 8, 'Huấn luyện viên thể thao chuyên nghiệp'),
    ('Lê Thị Hoa', 'hoa.le@center.com', '0901234569', 'Nghệ thuật', 6, 'Giáo viên dạy vẽ và thủ công cho trẻ em'),
    ('Phạm Văn Đức', 'duc.pham@center.com', '0901234570', 'Khoa học', 4, 'Giáo viên khoa học, chuyên thí nghiệm vui');

-- Thêm hoạt động mẫu
INSERT INTO "Activities" ("Title", "Description", "Type", "Price", "MaxParticipants", "MinAge", "MaxAge", "Skills", "Location", "StartDate", "EndDate", "StartTime", "EndTime", "TeacherId", "Status")
VALUES 
    ('Khóa học kỹ năng giao tiếp', 'Dạy trẻ em cách giao tiếp tự tin và hiệu quả', 'free', 0, 15, 8, 14, 'Giao tiếp, Tự tin, Thuyết trình', 'Phòng A1 - Trung tâm', '2024-07-15', '2024-07-19', '08:00', '11:00', 1, 'Published'),
    ('Trại hè thể thao', 'Các hoạt động thể thao ngoài trời và trong nhà', 'paid', 500000, 20, 10, 16, 'Thể lực, Tinh thần đồng đội, Kỷ luật', 'Sân thể thao trung tâm', '2024-07-22', '2024-07-26', '07:30', '16:30', 2, 'Published'),
    ('Workshop vẽ tranh sáng tạo', 'Khám phá khả năng sáng tạo qua hội họa', 'paid', 300000, 12, 6, 12, 'Sáng tạo, Tư duy nghệ thuật, Kiên nhẫn', 'Phòng nghệ thuật', '2024-07-29', '2024-08-02', '14:00', '17:00', 3, 'Published'),
    ('Khám phá khoa học vui', 'Thí nghiệm khoa học đơn giản và thú vị', 'free', 0, 18, 8, 15, 'Tư duy logic, Quan sát, Khám phá', 'Phòng thí nghiệm', '2024-08-05', '2024-08-09', '09:00', '12:00', 4, 'Published'),
    ('Kỹ năng sống độc lập', 'Dạy trẻ các kỹ năng cần thiết trong cuộc sống', 'paid', 400000, 16, 12, 18, 'Tự lập, Quản lý thời gian, Tài chính cá nhân', 'Phòng đa năng', '2024-08-12', '2024-08-16', '08:30', '11:30', 1, 'Published');
