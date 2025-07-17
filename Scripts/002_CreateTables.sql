-- Tạo bảng AspNetUsers và Identity tables
CREATE TABLE "AspNetUsers" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "UserName" VARCHAR(256) NOT NULL,
    "NormalizedUserName" VARCHAR(256),
    "Email" VARCHAR(256),
    "NormalizedEmail" VARCHAR(256),
    "EmailConfirmed" BOOLEAN NOT NULL DEFAULT FALSE,
    "PasswordHash" VARCHAR(255),
    "SecurityStamp" VARCHAR(255),
    "ConcurrencyStamp" VARCHAR(255),
    "PhoneNumber" VARCHAR(255),
    "PhoneNumberConfirmed" BOOLEAN NOT NULL DEFAULT FALSE,
    "TwoFactorEnabled" BOOLEAN NOT NULL DEFAULT FALSE,
    "LockoutEnd" TIMESTAMP,
    "LockoutEnabled" BOOLEAN NOT NULL DEFAULT FALSE,
    "AccessFailedCount" INT NOT NULL DEFAULT 0,
    "FullName" VARCHAR(100),
    "DateOfBirth" DATE,
    "Address" TEXT,
    "ParentId" VARCHAR(255), -- Liên kết học sinh với phụ huynh
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP,
    FOREIGN KEY ("ParentId") REFERENCES "AspNetUsers"("Id") ON DELETE SET NULL
);

CREATE TABLE "AspNetRoles" (
    "Id" VARCHAR(255) PRIMARY KEY,
    "Name" VARCHAR(256) NOT NULL,
    "NormalizedName" VARCHAR(256),
    "ConcurrencyStamp" VARCHAR(255)
);

CREATE TABLE "AspNetUserRoles" (
    "UserId" VARCHAR(255) NOT NULL,
    "RoleId" VARCHAR(255) NOT NULL,
    PRIMARY KEY ("UserId", "RoleId"),
    FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles"("Id") ON DELETE CASCADE
);

CREATE TABLE "AspNetUserClaims" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" VARCHAR(255) NOT NULL,
    "ClaimType" VARCHAR(255),
    "ClaimValue" VARCHAR(255),
    FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE
);

CREATE TABLE "AspNetRoleClaims" (
    "Id" SERIAL PRIMARY KEY,
    "RoleId" VARCHAR(255) NOT NULL,
    "ClaimType" VARCHAR(255),
    "ClaimValue" VARCHAR(255),
    FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles"("Id") ON DELETE CASCADE
);

CREATE TABLE "AspNetUserLogins" (
    "LoginProvider" VARCHAR(255) NOT NULL,
    "ProviderKey" VARCHAR(255) NOT NULL,
    "ProviderDisplayName" VARCHAR(255),
    "UserId" VARCHAR(255) NOT NULL,
    PRIMARY KEY ("LoginProvider", "ProviderKey"),
    FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE
);

CREATE TABLE "AspNetUserTokens" (
    "UserId" VARCHAR(255) NOT NULL,
    "LoginProvider" VARCHAR(255) NOT NULL,
    "Name" VARCHAR(255) NOT NULL,
    "Value" VARCHAR(255),
    PRIMARY KEY ("UserId", "LoginProvider", "Name"),
    FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE
);

-- Bảng Giáo viên
CREATE TABLE "Teachers" (
    "TeacherId" SERIAL PRIMARY KEY,
    "FullName" VARCHAR(100) NOT NULL,
    "Email" VARCHAR(256),
    "PhoneNumber" VARCHAR(20),
    "Specialization" VARCHAR(100), -- Chuyên môn
    "Experience" INT DEFAULT 0, -- Số năm kinh nghiệm
    "Bio" TEXT, -- Tiểu sử
    "IsActive" BOOLEAN DEFAULT TRUE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP
);

-- Bảng Hoạt động
CREATE TABLE "Activities" (
    "ActivityId" SERIAL PRIMARY KEY,
    "Title" VARCHAR(255) NOT NULL,
    "Description" TEXT NOT NULL,
    "ImageUrl" VARCHAR(500),
    "Type" VARCHAR(50) NOT NULL DEFAULT 'free', -- 'free', 'paid'
    "Price" DECIMAL(10,2) NOT NULL DEFAULT 0,
    "MaxParticipants" INT NOT NULL DEFAULT 20, -- Số lượng tối đa
    "MinAge" INT DEFAULT 6, -- Tuổi tối thiểu
    "MaxAge" INT DEFAULT 18, -- Tuổi tối đa
    "Skills" TEXT, -- Kỹ năng sống được dạy
    "Requirements" TEXT, -- Yêu cầu tham gia
    "Location" VARCHAR(255), -- Địa điểm
    "StartDate" DATE NOT NULL,
    "EndDate" DATE NOT NULL,
    "StartTime" TIME NOT NULL,
    "EndTime" TIME NOT NULL,
    "TeacherId" INT,
    "Status" VARCHAR(50) DEFAULT 'Draft', -- Draft, Published, Full, Cancelled, Completed
    "CreatedBy" VARCHAR(255),
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP,
    FOREIGN KEY ("TeacherId") REFERENCES "Teachers"("TeacherId") ON DELETE SET NULL,
    FOREIGN KEY ("CreatedBy") REFERENCES "AspNetUsers"("Id") ON DELETE SET NULL
);

-- Bảng Đăng ký tham gia
CREATE TABLE "Registrations" (
    "RegistrationId" SERIAL PRIMARY KEY,
    "StudentId" VARCHAR(255) NOT NULL, -- ID của học sinh
    "ActivityId" INT NOT NULL,
    "ParentId" VARCHAR(255), -- ID của phụ huynh đăng ký
    "RegistrationDate" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Status" VARCHAR(50) DEFAULT 'Pending', -- Pending, Approved, Rejected, Cancelled
    "Notes" TEXT, -- Ghi chú từ phụ huynh
    "PaymentStatus" VARCHAR(50) DEFAULT 'Unpaid', -- Unpaid, Paid, Refunded
    "AttendanceStatus" VARCHAR(50) DEFAULT 'Registered', -- Registered, Attended, Absent
    FOREIGN KEY ("StudentId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    FOREIGN KEY ("ActivityId") REFERENCES "Activities"("ActivityId") ON DELETE CASCADE,
    FOREIGN KEY ("ParentId") REFERENCES "AspNetUsers"("Id") ON DELETE SET NULL,
    UNIQUE("StudentId", "ActivityId") -- Một học sinh chỉ đăng ký một lần cho mỗi hoạt động
);

-- Bảng Thanh toán
CREATE TABLE "Payments" (
    "PaymentId" SERIAL PRIMARY KEY,
    "RegistrationId" INT NOT NULL,
    "Amount" DECIMAL(10,2) NOT NULL,
    "PaymentDate" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "PaymentMethod" VARCHAR(50), -- Cash, Transfer, Online
    "TransactionId" VARCHAR(255),
    "PaymentStatus" VARCHAR(50) DEFAULT 'Pending',
    "ResponseCode" VARCHAR(50),
    "Notes" TEXT,
    FOREIGN KEY ("RegistrationId") REFERENCES "Registrations"("RegistrationId") ON DELETE CASCADE
);

-- Bảng Tin nhắn
CREATE TABLE "Messages" (
    "MessageId" SERIAL PRIMARY KEY,
    "SenderId" VARCHAR(255) NOT NULL,
    "RecipientId" VARCHAR(255), -- NULL nếu gửi cho admin
    "Subject" VARCHAR(255),
    "Content" TEXT NOT NULL,
    "IsRead" BOOLEAN DEFAULT FALSE,
    "MessageType" VARCHAR(50) DEFAULT 'General', -- General, Support, Registration, Payment
    "RelatedActivityId" INT, -- Liên quan đến hoạt động nào
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY ("SenderId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    FOREIGN KEY ("RecipientId") REFERENCES "AspNetUsers"("Id") ON DELETE SET NULL,
    FOREIGN KEY ("RelatedActivityId") REFERENCES "Activities"("ActivityId") ON DELETE SET NULL
);

-- Bảng Tương tác (Like, Comment)
CREATE TABLE "Interactions" (
    "InteractionId" SERIAL PRIMARY KEY,
    "UserId" VARCHAR(255) NOT NULL,
    "ActivityId" INT NOT NULL,
    "InteractionType" VARCHAR(50) NOT NULL, -- Like, Comment, Share
    "Content" TEXT, -- Nội dung comment
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    FOREIGN KEY ("ActivityId") REFERENCES "Activities"("ActivityId") ON DELETE CASCADE
);

-- Tạo indexes
CREATE INDEX "IX_Activities_StartDate" ON "Activities" ("StartDate");
CREATE INDEX "IX_Activities_Status" ON "Activities" ("Status");
CREATE INDEX "IX_Activities_TeacherId" ON "Activities" ("TeacherId");
CREATE INDEX "IX_Registrations_StudentId" ON "Registrations" ("StudentId");
CREATE INDEX "IX_Registrations_ActivityId" ON "Registrations" ("ActivityId");
CREATE INDEX "IX_Registrations_ParentId" ON "Registrations" ("ParentId");
CREATE INDEX "IX_Messages_SenderId" ON "Messages" ("SenderId");
CREATE INDEX "IX_Messages_RecipientId" ON "Messages" ("RecipientId");
CREATE INDEX "IX_Interactions_ActivityId" ON "Interactions" ("ActivityId");
CREATE INDEX "IX_AspNetUsers_ParentId" ON "AspNetUsers" ("ParentId");
