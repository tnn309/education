CREATE TABLE IF NOT EXISTS "AspNetRoles" (
    "Id" TEXT NOT NULL PRIMARY KEY,
    "Name" VARCHAR(256) NULL,
    "NormalizedName" VARCHAR(256) NULL,
    "ConcurrencyStamp" TEXT NULL
);

CREATE TABLE IF NOT EXISTS "AspNetRoleClaims" (
    "Id" SERIAL PRIMARY KEY,
    "RoleId" TEXT NOT NULL,
    "ClaimType" TEXT NULL,
    "ClaimValue" TEXT NULL,
    CONSTRAINT "FK_AspNetRoleClaims_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "AspNetUsers" (
    "Id" TEXT NOT NULL PRIMARY KEY,
    "UserName" VARCHAR(256) NULL,
    "NormalizedUserName" VARCHAR(256) NULL,
    "Email" VARCHAR(256) NULL,
    "NormalizedEmail" VARCHAR(256) NULL,
    "EmailConfirmed" BOOLEAN NOT NULL,
    "PasswordHash" TEXT NULL,
    "SecurityStamp" TEXT NULL,
    "ConcurrencyStamp" TEXT NULL,
    "PhoneNumber" TEXT NULL,
    "PhoneNumberConfirmed" BOOLEAN NOT NULL,
    "TwoFactorEnabled" BOOLEAN NOT NULL,
    "LockoutEnd" TIMESTAMP WITH TIME ZONE NULL,
    "LockoutEnabled" BOOLEAN NOT NULL,
    "AccessFailedCount" INT NOT NULL,
    -- Custom columns from ApplicationUser model
    "FullName" VARCHAR(255) NULL,
    "DateOfBirth" DATE NULL,
    "Address" VARCHAR(500) NULL,
    "ParentId" TEXT NULL, -- Self-referencing FK to AspNetUsers.Id
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_AspNetUsers_AspNetUsers_ParentId" FOREIGN KEY ("ParentId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS "AspNetUserClaims" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" TEXT NOT NULL,
    "ClaimType" TEXT NULL,
    "ClaimValue" TEXT NULL,
    CONSTRAINT "FK_AspNetUserClaims_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "AspNetUserLogins" (
    "LoginProvider" TEXT NOT NULL,
    "ProviderKey" TEXT NOT NULL,
    "ProviderDisplayName" TEXT NULL,
    "UserId" TEXT NOT NULL,
    CONSTRAINT "PK_AspNetUserLogins" PRIMARY KEY ("LoginProvider", "ProviderKey"),
    CONSTRAINT "FK_AspNetUserLogins_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "AspNetUserRoles" (
    "UserId" TEXT NOT NULL,
    "RoleId" TEXT NOT NULL,
    CONSTRAINT "PK_AspNetUserRoles" PRIMARY KEY ("UserId", "RoleId"),
    CONSTRAINT "FK_AspNetUserRoles_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_AspNetUserRoles_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "AspNetUserTokens" (
    "UserId" TEXT NOT NULL,
    "LoginProvider" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    "Value" TEXT NULL,
    CONSTRAINT "PK_AspNetUserTokens" PRIMARY KEY ("UserId", "LoginProvider", "Name"),
    CONSTRAINT "FK_AspNetUserTokens_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

-- 2. Teachers Table
CREATE TABLE IF NOT EXISTS "Teachers" (
    "TeacherId" SERIAL PRIMARY KEY,
    "FullName" VARCHAR(255) NOT NULL,
    "Email" VARCHAR(255) NULL,
    "PhoneNumber" VARCHAR(20) NULL,
    "Specialization" VARCHAR(255) NULL,
    "Experience" INT NOT NULL DEFAULT 0,
    "Bio" TEXT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 3. Activities Table
CREATE TABLE IF NOT EXISTS "Activities" (
    "ActivityId" SERIAL PRIMARY KEY,
    "Title" VARCHAR(255) NOT NULL,
    "Description" TEXT NOT NULL,
    "Type" VARCHAR(50) NOT NULL DEFAULT 'free', -- 'free' or 'paid'
    "Price" DECIMAL(18, 2) NOT NULL DEFAULT 0.00,
    "ImageUrl" VARCHAR(255) NULL,
    "MaxParticipants" INT NOT NULL,
    "CurrentParticipants" INT NOT NULL DEFAULT 0,
    "MinAge" INT NOT NULL,
    "MaxAge" INT NOT NULL,
    "Location" VARCHAR(255) NOT NULL,
    "StartDate" DATE NOT NULL,
    "EndDate" DATE NOT NULL,
    "StartTime" TIME NOT NULL,
    "EndTime" TIME NOT NULL,
    "Skills" TEXT NULL,
    "Requirements" TEXT NULL,
    "TeacherId" INT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "IsFull" BOOLEAN NOT NULL DEFAULT FALSE,
    "LikesCount" INT NOT NULL DEFAULT 0,
    "CommentsCount" INT NOT NULL DEFAULT 0,
    "Status" VARCHAR(50) NOT NULL DEFAULT 'Published', -- 'Draft', 'Published', 'Archived', 'Full'
    "CreatorId" TEXT NULL, -- FK to AspNetUsers.Id
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_Activities_Teachers_TeacherId" FOREIGN KEY ("TeacherId") REFERENCES "Teachers" ("TeacherId") ON DELETE SET NULL,
    CONSTRAINT "FK_Activities_AspNetUsers_CreatorId" FOREIGN KEY ("CreatorId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT
);

-- 4. Registrations Table
CREATE TABLE IF NOT EXISTS "Registrations" (
    "RegistrationId" SERIAL PRIMARY KEY,
    "ActivityId" INT NOT NULL,
    "StudentId" TEXT NOT NULL, -- FK to AspNetUsers.Id (the student)
    "ParentId" TEXT NULL, -- FK to AspNetUsers.Id (the parent, if student has one)
    "UserId" TEXT NULL, -- Compatibility property, also FK to AspNetUsers.Id
    "RegistrationDate" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Status" VARCHAR(50) NOT NULL DEFAULT 'Pending', -- 'Pending', 'Approved', 'Rejected', 'Cancelled'
    "PaymentStatus" VARCHAR(50) NOT NULL DEFAULT 'Unpaid', -- 'Paid', 'Unpaid', 'Refunded', 'N/A'
    "Notes" TEXT NULL,
    "AttendanceStatus" VARCHAR(50) NOT NULL DEFAULT 'Not Started', -- 'Not Started', 'Present', 'Absent', 'Completed'
    "AmountPaid" DECIMAL(18, 2) NULL,
    CONSTRAINT "FK_Registrations_Activities_ActivityId" FOREIGN KEY ("ActivityId") REFERENCES "Activities" ("ActivityId") ON DELETE CASCADE,
    CONSTRAINT "FK_Registrations_AspNetUsers_StudentId" FOREIGN KEY ("StudentId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Registrations_AspNetUsers_ParentId" FOREIGN KEY ("ParentId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Registrations_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT
);

-- 5. Interactions Table (for Likes and Comments)
CREATE TABLE IF NOT EXISTS "Interactions" (
    "InteractionId" SERIAL PRIMARY KEY,
    "ActivityId" INT NOT NULL,
    "UserId" TEXT NOT NULL, -- FK to AspNetUsers.Id
    "InteractionType" VARCHAR(50) NOT NULL, -- 'Like' or 'Comment'
    "Content" TEXT NULL, -- For comments
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "FK_Interactions_Activities_ActivityId" FOREIGN KEY ("ActivityId") REFERENCES "Activities" ("ActivityId") ON DELETE CASCADE,
    CONSTRAINT "FK_Interactions_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT
);

-- 6. CartItems Table
CREATE TABLE IF NOT EXISTS "CartItems" (
    "CartItemId" SERIAL PRIMARY KEY,
    "UserId" TEXT NOT NULL, -- FK to AspNetUsers.Id
    "ActivityId" INT NOT NULL, -- FK to Activities.ActivityId
    "AddedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "IsPaid" BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT "FK_CartItems_Activities_ActivityId" FOREIGN KEY ("ActivityId") REFERENCES "Activities" ("ActivityId") ON DELETE CASCADE,
    CONSTRAINT "FK_CartItems_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

-- 7. Payments Table
CREATE TABLE IF NOT EXISTS "Payments" (
    "PaymentId" SERIAL PRIMARY KEY,
    "RegistrationId" INT NOT NULL,
    "UserId" TEXT NOT NULL, -- Who made the payment (FK to AspNetUsers.Id)
    "Amount" DECIMAL(18, 2) NOT NULL,
    "PaymentMethod" VARCHAR(50) NOT NULL, -- "Cash", "Card", "Transfer"
    "PaymentStatus" VARCHAR(50) NOT NULL, -- "Pending", "Completed", "Failed", "Refunded"
    "TransactionId" VARCHAR(255) NULL,
    "ResponseCode" VARCHAR(255) NULL,
    "PaymentDate" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Notes" TEXT NULL,
    CONSTRAINT "FK_Payments_Registrations_RegistrationId" FOREIGN KEY ("RegistrationId") REFERENCES "Registrations" ("RegistrationId") ON DELETE CASCADE,
    CONSTRAINT "FK_Payments_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT
);

-- 8. Messages Table
CREATE TABLE IF NOT EXISTS "Messages" (
    "MessageId" SERIAL PRIMARY KEY,
    "SenderId" TEXT NOT NULL, -- FK to AspNetUsers.Id
    "ReceiverId" TEXT NOT NULL, -- FK to AspNetUsers.Id
    "ActivityId" INT NULL, -- Optional: related to specific activity
    "Subject" VARCHAR(255) NOT NULL,
    "Content" TEXT NOT NULL,
    "IsRead" BOOLEAN NOT NULL DEFAULT FALSE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "ReadAt" TIMESTAMP NULL,
    "MessageType" VARCHAR(50) NULL,
    CONSTRAINT "FK_Messages_AspNetUsers_SenderId" FOREIGN KEY ("SenderId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Messages_AspNetUsers_ReceiverId" FOREIGN KEY ("ReceiverId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Messages_Activities_ActivityId" FOREIGN KEY ("ActivityId") REFERENCES "Activities" ("ActivityId") ON DELETE SET NULL
);

-- 9. Indexes for performance optimization
-- Identity indexes
CREATE INDEX IF NOT EXISTS "IX_AspNetRoleClaims_RoleId" ON "AspNetRoleClaims" ("RoleId");
CREATE UNIQUE INDEX IF NOT EXISTS "RoleNameIndex" ON "AspNetRoles" ("NormalizedName");
CREATE INDEX IF NOT EXISTS "IX_AspNetUserClaims_UserId" ON "AspNetUserClaims" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_AspNetUserLogins_UserId" ON "AspNetUserLogins" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_AspNetUserRoles_RoleId" ON "AspNetUserRoles" ("RoleId");
CREATE INDEX IF NOT EXISTS "EmailIndex" ON "AspNetUsers" ("NormalizedEmail");
CREATE UNIQUE INDEX IF NOT EXISTS "UserNameIndex" ON "AspNetUsers" ("NormalizedUserName");

-- Custom ApplicationUser indexes
CREATE INDEX IF NOT EXISTS "IX_AspNetUsers_ParentId" ON "AspNetUsers" ("ParentId");

-- Teachers indexes
CREATE INDEX IF NOT EXISTS "IX_Teachers_FullName" ON "Teachers" ("FullName");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Teachers_Email" ON "Teachers" ("Email");

-- Activities indexes
CREATE INDEX IF NOT EXISTS "IX_Activities_TeacherId" ON "Activities" ("TeacherId");
CREATE INDEX IF NOT EXISTS "IX_Activities_CreatorId" ON "Activities" ("CreatorId");
CREATE INDEX IF NOT EXISTS "IX_Activities_Title" ON "Activities" ("Title");
CREATE INDEX IF NOT EXISTS "IX_Activities_Type" ON "Activities" ("Type");
CREATE INDEX IF NOT EXISTS "IX_Activities_StartDate" ON "Activities" ("StartDate");
CREATE INDEX IF NOT EXISTS "IX_Activities_Status" ON "Activities" ("Status");

-- Registrations indexes
CREATE INDEX IF NOT EXISTS "IX_Registrations_ActivityId" ON "Registrations" ("ActivityId");
CREATE INDEX IF NOT EXISTS "IX_Registrations_StudentId" ON "Registrations" ("StudentId");
CREATE INDEX IF NOT EXISTS "IX_Registrations_ParentId" ON "Registrations" ("ParentId");
CREATE INDEX IF NOT EXISTS "IX_Registrations_UserId" ON "Registrations" ("UserId"); -- For compatibility
CREATE INDEX IF NOT EXISTS "IX_Registrations_Status" ON "Registrations" ("Status");
CREATE INDEX IF NOT EXISTS "IX_Registrations_PaymentStatus" ON "Registrations" ("PaymentStatus");

-- Interactions indexes
CREATE INDEX IF NOT EXISTS "IX_Interactions_ActivityId" ON "Interactions" ("ActivityId");
CREATE INDEX IF NOT EXISTS "IX_Interactions_UserId" ON "Interactions" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_Interactions_InteractionType" ON "Interactions" ("InteractionType");

-- CartItems indexes
CREATE INDEX IF NOT EXISTS "IX_CartItems_UserId" ON "CartItems" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_CartItems_ActivityId" ON "CartItems" ("ActivityId");

-- Payments indexes
CREATE INDEX IF NOT EXISTS "IX_Payments_RegistrationId" ON "Payments" ("RegistrationId");
CREATE INDEX IF NOT EXISTS "IX_Payments_UserId" ON "Payments" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_Payments_PaymentStatus" ON "Payments" ("PaymentStatus");

-- Messages indexes
CREATE INDEX IF NOT EXISTS "IX_Messages_SenderId" ON "Messages" ("SenderId");
CREATE INDEX IF NOT EXISTS "IX_Messages_ReceiverId" ON "Messages" ("ReceiverId");
CREATE INDEX IF NOT EXISTS "IX_Messages_ActivityId" ON "Messages" ("ActivityId");
CREATE INDEX IF NOT EXISTS "IX_Messages_CreatedAt" ON "Messages" ("CreatedAt");
