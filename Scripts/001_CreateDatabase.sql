-- Xóa và tạo lại database
DROP DATABASE IF EXISTS "SummerActivitySystem";
CREATE DATABASE "SummerActivitySystem"
WITH
  OWNER = postgres
  ENCODING = 'UTF8'
  CONNECTION LIMIT = -1;

-- Kết nối vào database mới tạo và chạy script tiếp theo
