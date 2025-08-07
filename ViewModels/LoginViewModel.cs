using System.ComponentModel.DataAnnotations;        

namespace EducationSystem.ViewModels                                    // ViewModel cho trang đăng nhập
{
    public class LoginViewModel                 
    {
        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]          // Không nhập tên thì báo lỗi
        [Display(Name = "Tên đăng nhập")]                               
        public string Username { get; set; } = string.Empty;            // Tên đăng nhập của người dùng

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]               // Không nhâp mật khẩu thì báo lỗi
        [DataType(DataType.Password)]                                   // Hash mật khẩu
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool RememberMe { get; set; }                            // Ghi nhớ đăng nhập hay không
    }
}
