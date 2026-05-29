$files = @(
  'e:\pbl3\flight\frontend\src\App.jsx',
  'e:\pbl3\flight\frontend\src\api.js',
  'e:\pbl3\flight\frontend\src\components\admin\AdminDashboard.jsx',
  'e:\pbl3\flight\frontend\src\components\admin\FlightListWithPagination.jsx',
  'e:\pbl3\flight\frontend\src\components\flight-templates\FlightTemplateManagement.jsx',
  'e:\pbl3\flight\frontend\src\components\flight-templates\FlightTemplateDetailTable.jsx',
  'e:\pbl3\flight\frontend\src\components\flight-templates\FlightTemplateDetailForm.jsx',
  'e:\pbl3\flight\frontend\src\components\flight-templates\FlightTemplateForm.jsx',
  'e:\pbl3\flight\frontend\src\components\flight-templates\FlightTemplateList.jsx',
  'e:\pbl3\flight\frontend\src\components\flight-templates\GenerateFlightsModal.jsx',
  'e:\pbl3\flight\frontend\src\services\flightTemplateApi.js',
  'e:\pbl3\flight\frontend\src\index.css',
  'e:\pbl3\flight\frontend\src\App.css',
  'e:\pbl3\flight\frontend\src\components\common\Header.jsx',
  'e:\pbl3\flight\frontend\src\components\common\Footer.jsx',
  'e:\pbl3\flight\frontend\src\components\auth\Login.jsx',
  'e:\pbl3\flight\frontend\src\components\auth\Register.jsx',
  'e:\pbl3\flight\frontend\src\components\booking\FlightSearchForm.jsx',
  'e:\pbl3\flight\frontend\src\components\booking\FlightSearchResults.jsx',
  'e:\pbl3\flight\frontend\src\components\booking\PassengerDetailsForm.jsx',
  'e:\pbl3\flight\frontend\src\components\booking\PaymentSummary.jsx',
  'e:\pbl3\flight\frontend\src\components\booking\BookingHistory.jsx',
  'e:\pbl3\flight\frontend\src\components\booking\PaymentHistory.jsx',
  'e:\pbl3\flight\frontend\src\components\booking\SavedPassengers.jsx',
  'e:\pbl3\flight\frontend\src\components\auth\ForgotPassword.jsx',
  'e:\pbl3\flight\frontend\src\components\auth\ResetPassword.jsx'
)

$latin1 = [System.Text.Encoding]::GetEncoding('iso-8859-1')
$garbledPattern = [System.Text.RegularExpressions.Regex]'[\xC3\xC6][\x80-\xBF]|[\xE1-\xEF][\x80-\xBF]{2}'

foreach ($file in $files) {
  if (-not (Test-Path $file)) {
    Write-Host "NOT FOUND: $file"
    continue
  }
  $rawBytes = [System.IO.File]::ReadAllBytes($file)
  $utf8Text = [System.Text.Encoding]::UTF8.GetString($rawBytes)

  # Detect mojibake: typical pattern is Ã followed by Latin chars
  $hasMojibake = ($utf8Text -match 'Ã[€-þ]') -or ($utf8Text -match 'á[»º¸¹½¼¾¿]') -or ($utf8Text -match 'â€[œ™]')

  if ($hasMojibake) {
    Write-Host "FIXING: $file"
    try {
      $latin1Bytes = $latin1.GetBytes($utf8Text)
      $fixed = [System.Text.Encoding]::UTF8.GetString($latin1Bytes)
      [System.IO.File]::WriteAllText($file, $fixed, [System.Text.Encoding]::UTF8NoBOM)
      Write-Host "  => Fixed successfully"
    } catch {
      Write-Host "  => ERROR: $_"
    }
  } else {
    Write-Host "OK (clean): $file"
  }
}

Write-Host ""
Write-Host "=== ENCODING FIX COMPLETE ==="
