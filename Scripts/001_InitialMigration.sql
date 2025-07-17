-- Migration script for PostgreSQL
-- Run this after creating the database

-- Create roles if they don't exist
INSERT INTO "AspNetRoles" ("Id", "Name", "NormalizedName", "ConcurrencyStamp")
VALUES 
    ('1', 'Admin', 'ADMIN', NEWID()::text),
    ('2', 'Student', 'STUDENT', NEWID()::text),
    ('3', 'Parent', 'PARENT', NEWID()::text)
ON CONFLICT ("Id") DO NOTHING;

-- Create indexes for better performance
CREATE INDEX IF NOT EXISTS "IX_Activities_Type" ON "Activities" ("Type");
CREATE INDEX IF NOT EXISTS "IX_Activities_CreatedAt" ON "Activities" ("CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_Activities_UserId" ON "Activities" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_Registrations_UserId" ON "Registrations" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_Registrations_ActivityId" ON "Registrations" ("ActivityId");
CREATE INDEX IF NOT EXISTS "IX_CartItems_UserId" ON "CartItems" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_CartItems_ActivityId" ON "CartItems" ("ActivityId");
CREATE INDEX IF NOT EXISTS "IX_Payments_RegistrationId" ON "Payments" ("RegistrationId");

-- Insert sample data
INSERT INTO "Activities" ("Title", "Description", "Type", "Price", "CreatedBy", "ImageUrl")
VALUES 
    ('Khóa học lập trình cơ bản', 'Học lập trình từ cơ bản đến nâng cao', 'free', 0, 'admin', '/images/programming.jpg'),
    ('Khóa học tiếng Anh giao tiếp', 'Cải thiện kỹ năng giao tiếp tiếng Anh', 'paid', 500000, 'admin', '/images/english.jpg'),
    ('Workshop thiết kế UI/UX', 'Tìm hiểu về thiết kế giao diện người dùng', 'paid', 300000, 'admin', '/images/uiux.jpg'),
    ('Khóa học toán học cơ bản', 'Ôn tập và nâng cao kiến thức toán học', 'free', 0, 'admin', '/images/math.jpg');
