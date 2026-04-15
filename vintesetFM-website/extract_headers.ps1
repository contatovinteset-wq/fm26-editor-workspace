$excel = New-Object -ComObject Excel.Application
$excel.Visible = $false
try {
   $wb = $excel.Workbooks.Open('C:\Users\Raphael\Downloads\Allan FCL - Moneyball FM26 (1)\1. Planilha - Moneyball\Moneyball FM26 - Avancados.xlsm')
   foreach ($sheet in $wb.Sheets) {
       Write-Output "=== Sheet: $($sheet.Name) ==="
       $range = $sheet.Range("A1:DZ1")
       $vals = $range.Value2
       if ($vals -ne $null) {
           for ($c = 1; $c -le 80; $c++) {
               $hf = $vals[1, $c]
               if ($hf) {
                  Write-Output "Col $c : $hf"
               }
           }
       }
   }
} finally {
   $wb.Close($false)
   $excel.Quit()
}
