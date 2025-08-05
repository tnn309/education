// EducationSys JavaScript Functions

// Import Bootstrap
const bootstrap = window.bootstrap

// Initialize tooltips and popovers
document.addEventListener("DOMContentLoaded", () => {
  // Initialize Bootstrap tooltips
  var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'))
  var tooltipList = tooltipTriggerList.map((tooltipTriggerEl) => new bootstrap.Tooltip(tooltipTriggerEl))

  // Initialize Bootstrap popovers
  var popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'))
  var popoverList = popoverTriggerList.map((popoverTriggerEl) => new bootstrap.Popover(popoverTriggerEl))

  // Auto-hide alerts after 5 seconds
  setTimeout(() => {
    var alerts = document.querySelectorAll(".alert")
    alerts.forEach((alert) => {
      var bsAlert = new bootstrap.Alert(alert)
      bsAlert.close()
    })
  }, 5000)

  // Add fade-in animation to cards
  var cards = document.querySelectorAll(".card")
  cards.forEach((card, index) => {
    card.style.animationDelay = index * 0.1 + "s"
    card.classList.add("fade-in")
  })
})

// Utility Functions
function formatCurrency(amount) {
  return new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
  }).format(amount)
}

function formatDate(dateString) {
  const date = new Date(dateString)
  return date.toLocaleDateString("vi-VN")
}

function formatDateTime(dateString) {
  const date = new Date(dateString)
  return date.toLocaleString("vi-VN")
}

// Confirmation dialogs
function confirmDelete(message) {
  return confirm(message || "Bạn có chắc chắn muốn xóa?")
}

function confirmAction(message) {
  return confirm(message || "Bạn có chắc chắn muốn thực hiện hành động này?")
}

// Loading states
function showLoading(element) {
  if (element) {
    element.disabled = true
    element.innerHTML = '<span class="loading"></span> Đang xử lý...'
  }
}

function hideLoading(element, originalText) {
  if (element) {
    element.disabled = false
    element.innerHTML = originalText || "Hoàn thành"
  }
}

// Form validation helpers
function validateEmail(email) {
  const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
  return re.test(email)
}

function validatePhone(phone) {
  const re = /^[0-9]{10,11}$/
  return re.test(phone.replace(/\s/g, ""))
}

// Search functionality
function debounce(func, wait) {
  let timeout
  return function executedFunction(...args) {
    const later = () => {
      clearTimeout(timeout)
      func(...args)
    }
    clearTimeout(timeout)
    timeout = setTimeout(later, wait)
  }
}

// Activity card interactions
document.addEventListener("DOMContentLoaded", () => {
  const activityCards = document.querySelectorAll(".activity-card")

  activityCards.forEach((card) => {
    card.addEventListener("mouseenter", function () {
      this.style.transform = "translateY(-5px)"
      this.style.transition = "transform 0.3s ease"
    })

    card.addEventListener("mouseleave", function () {
      this.style.transform = "translateY(0)"
    })
  })
})

// Smooth scrolling for anchor links
document.addEventListener("DOMContentLoaded", () => {
  const links = document.querySelectorAll('a[href^="#"]')

  links.forEach((link) => {
    link.addEventListener("click", function (e) {
      e.preventDefault()

      const targetId = this.getAttribute("href")
      const targetElement = document.querySelector(targetId)

      if (targetElement) {
        targetElement.scrollIntoView({
          behavior: "smooth",
          block: "start",
        })
      }
    })
  })
})

// Back to top button
document.addEventListener("DOMContentLoaded", () => {
  // Create back to top button
  const backToTopButton = document.createElement("button")
  backToTopButton.innerHTML = '<i class="fas fa-arrow-up"></i>'
  backToTopButton.className = "btn btn-primary position-fixed"
  backToTopButton.style.cssText = `
        bottom: 20px;
        right: 20px;
        z-index: 1000;
        border-radius: 50%;
        width: 50px;
        height: 50px;
        display: none;
        box-shadow: 0 4px 8px rgba(0,0,0,0.2);
    `
  backToTopButton.title = "Về đầu trang"

  document.body.appendChild(backToTopButton)

  // Show/hide button based on scroll position
  window.addEventListener("scroll", () => {
    if (window.pageYOffset > 300) {
      backToTopButton.style.display = "block"
    } else {
      backToTopButton.style.display = "none"
    }
  })

  // Scroll to top when clicked
  backToTopButton.addEventListener("click", () => {
    window.scrollTo({
      top: 0,
      behavior: "smooth",
    })
  })
})

// Local storage helpers
function saveToLocalStorage(key, data) {
  try {
    localStorage.setItem(key, JSON.stringify(data))
    return true
  } catch (error) {
    console.error("Error saving to localStorage:", error)
    return false
  }
}

function getFromLocalStorage(key) {
  try {
    const data = localStorage.getItem(key)
    return data ? JSON.parse(data) : null
  } catch (error) {
    console.error("Error reading from localStorage:", error)
    return null
  }
}

function removeFromLocalStorage(key) {
  try {
    localStorage.removeItem(key)
    return true
  } catch (error) {
    console.error("Error removing from localStorage:", error)
    return false
  }
}

// Theme toggle (for future dark mode implementation)
function toggleTheme() {
  const body = document.body
  const isDark = body.classList.contains("dark-theme")

  if (isDark) {
    body.classList.remove("dark-theme")
    saveToLocalStorage("theme", "light")
  } else {
    body.classList.add("dark-theme")
    saveToLocalStorage("theme", "dark")
  }
}

// Initialize theme on page load
document.addEventListener("DOMContentLoaded", () => {
  const savedTheme = getFromLocalStorage("theme")
  if (savedTheme === "dark") {
    document.body.classList.add("dark-theme")
  }
})

// Error handling for images
document.addEventListener("DOMContentLoaded", () => {
  const images = document.querySelectorAll("img")

  images.forEach((img) => {
    img.addEventListener("error", function () {
      this.src = "/placeholder.svg?height=200&width=300&text=Không thể tải hình ảnh"
      this.alt = "Hình ảnh không khả dụng"
    })
  })
})

// Copy to clipboard functionality
function copyToClipboard(text) {
  if (navigator.clipboard) {
    navigator.clipboard
      .writeText(text)
      .then(() => {
        showToast("Đã sao chép vào clipboard!", "success")
      })
      .catch((err) => {
        console.error("Could not copy text: ", err)
        showToast("Không thể sao chép!", "error")
      })
  } else {
    // Fallback for older browsers
    const textArea = document.createElement("textarea")
    textArea.value = text
    document.body.appendChild(textArea)
    textArea.select()
    try {
      document.execCommand("copy")
      showToast("Đã sao chép vào clipboard!", "success")
    } catch (err) {
      console.error("Could not copy text: ", err)
      showToast("Không thể sao chép!", "error")
    }
    document.body.removeChild(textArea)
  }
}

// Toast notifications
function showToast(message, type = "info", duration = 3000) {
  const toast = document.createElement("div")
  toast.className = `alert alert-${type === "error" ? "danger" : type} position-fixed`
  toast.style.cssText = `
        top: 20px;
        right: 20px;
        z-index: 1050;
        min-width: 300px;
        box-shadow: 0 4px 8px rgba(0,0,0,0.2);
    `
  toast.innerHTML = `
        ${message}
        <button type="button" class="btn-close" onclick="this.parentElement.remove()"></button>
    `

  document.body.appendChild(toast)

  // Auto remove after duration
  setTimeout(() => {
    if (toast.parentElement) {
      toast.remove()
    }
  }, duration)
}

// Print functionality
function printPage() {
  window.print()
}

function printElement(elementId) {
  const element = document.getElementById(elementId)
  if (element) {
    const printWindow = window.open("", "_blank")
    printWindow.document.write(`
            <html>
                <head>
                    <title>In tài liệu</title>
                    <link rel="stylesheet" href="/css/site.css">
                    <style>
                        body { margin: 20px; }
                        @media print { 
                            .no-print { display: none !important; }
                        }
                    </style>
                </head>
                <body>
                    ${element.innerHTML}
                </body>
            </html>
        `)
    printWindow.document.close()
    printWindow.print()
  }
}

// Export functionality
function exportToCSV(data, filename) {
  const csv = data.map((row) => row.join(",")).join("\n")
  const blob = new Blob([csv], { type: "text/csv" })
  const url = window.URL.createObjectURL(blob)
  const a = document.createElement("a")
  a.href = url
  a.download = filename || "export.csv"
  a.click()
  window.URL.revokeObjectURL(url)
}

// Initialize all functionality when DOM is ready
document.addEventListener("DOMContentLoaded", () => {
  console.log("EducationSys initialized successfully!")
})
