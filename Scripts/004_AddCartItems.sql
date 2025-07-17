-- Thêm bảng CartItems
CREATE TABLE IF NOT EXISTS "CartItems" (
    "CartItemId" SERIAL PRIMARY KEY,
    "UserId" VARCHAR(255),
    "ActivityId" INT NOT NULL,
    "AddedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "IsPaid" BOOLEAN DEFAULT FALSE,
    FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    FOREIGN KEY ("ActivityId") REFERENCES "Activities"("ActivityId") ON DELETE CASCADE,
    UNIQUE("UserId", "ActivityId") -- Một user chỉ thêm một hoạt động vào giỏ hàng một lần
);

-- Tạo index cho CartItems
CREATE INDEX IF NOT EXISTS "IX_CartItems_UserId" ON "CartItems" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_CartItems_ActivityId" ON "CartItems" ("ActivityId");
