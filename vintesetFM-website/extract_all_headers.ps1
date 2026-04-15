$excel = New-Object -ComObject Excel.Application
$excel.Visible = $false
try {
   $wb = $excel.Workbooks.Open('C:\Users\Raphael\Downloads\Allan FCL - Moneyball FM26 (1)\1. Planilha - Moneyball\Moneyball FM26 - Avancados.xlsm')
   $sheet = $wb.Sheets.Item(2)
   foreach ($s in $wb.Sheets) {
       if ($s.Name -match "Avan") {
           $sheet = $s
       }
   }
   
   $headersRows = $sheet.Range("A1:ZZ1").Value2
   for ($c = 1; $c -le 702; $c++) {
       $h = $headersRows[1, $c]
       if ($h) {
           Write-Output "$c : $h"
       }
   }
} finally {
   $wb.Close($false)
   $excel.Quit()
}
