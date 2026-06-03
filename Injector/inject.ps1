# SSJJ Plugin Injector - PowerShell
# Run as Administrator

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   SSJJ Plugin Injector (PowerShell)   " -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$dllPath = "D:\ssjj\Plugin\Robin\bin\x64\Release\Plugins.dll"
if (-not (Test-Path $dllPath)) {
    $dllPath = "D:\ssjj\Plugin\Robin\bin\Debug\Plugins.dll"
}
if (-not (Test-Path $dllPath)) {
    Write-Host "[!] Plugin DLL not found." -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host "[*] DLL: $dllPath" -ForegroundColor White

# Find game process
Write-Host "[*] Searching for game process..." -ForegroundColor White
$proc = $null

# Try exact name first
$proc = Get-Process -Name "SSJJ_BattleClient_Unity" -ErrorAction SilentlyContinue | Select-Object -First 1

# Fallback: find any process with mono.dll
if (-not $proc) {
    $proc = Get-Process | Where-Object {
        try {
            $_.Modules | Where-Object { $_.ModuleName -match "mono" } | Select-Object -First 1
        } catch { $null }
    } | Where-Object { $_.ProcessName -notmatch "Weixin|QQ|Discord" } | Select-Object -First 1
}

if (-not $proc) {
    Write-Host "[!] Game process with mono.dll not found." -ForegroundColor Red
    Write-Host "[*] Make sure the game is running." -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host "[+] Found: $($proc.ProcessName) (PID: $($proc.Id))" -ForegroundColor Green

# Add P/Invoke signatures
Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;

public static class InjectorApi {
    public const uint PROCESS_ALL_ACCESS = 0x1FFFFF;
    public const uint MEM_COMMIT = 0x1000;
    public const uint MEM_RESERVE = 0x2000;
    public const uint PAGE_READWRITE = 0x04;
    public const uint PAGE_EXECUTE_READWRITE = 0x40;

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr OpenProcess(uint access, bool inherit, int pid);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool CloseHandle(IntPtr handle);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr VirtualAllocEx(IntPtr proc, IntPtr addr, uint size, uint type, uint protect);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool VirtualFreeEx(IntPtr proc, IntPtr addr, uint size, uint type);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool WriteProcessMemory(IntPtr proc, IntPtr addr, byte[] buf, uint size, out int written);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr CreateRemoteThread(IntPtr proc, IntPtr attr, uint stack, IntPtr start, IntPtr param, uint flags, out uint tid);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern uint WaitForSingleObject(IntPtr handle, uint ms);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr LoadLibraryW(string name);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
    public static extern IntPtr GetProcAddress(IntPtr module, string name);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool FreeLibrary(IntPtr module);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool ReadProcessMemory(IntPtr proc, IntPtr addr, byte[] buf, uint size, out int read);
}
"@ -ReferencedAssemblies System.Runtime.InteropServices

$hProc = [InjectorApi]::OpenProcess([InjectorApi]::PROCESS_ALL_ACCESS, $false, $proc.Id)
if ($hProc -eq [IntPtr]::Zero) {
    Write-Host "[!] Failed to open process. Run as Administrator." -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

# Find mono.dll in target
$monoModule = $null
try { $monoModule = $proc.Modules | Where-Object { $_.ModuleName -match "mono" } | Select-Object -First 1 } catch {}
if (-not $monoModule) {
    Write-Host "[!] mono.dll not found in target." -ForegroundColor Red
    [InjectorApi]::CloseHandle($hProc)
    exit 1
}

$monoBase = $monoModule.BaseAddress
Write-Host "[+] mono.dll base: 0x$($monoBase.ToString('X'))" -ForegroundColor Green

# Load mono.dll locally to get function addresses
$monoPath = $monoModule.FileName
$localMono = [InjectorApi]::LoadLibraryW($monoPath)
if ($localMono -eq [IntPtr]::Zero) {
    Write-Host "[!] Failed to load mono.dll locally." -ForegroundColor Red
    [InjectorApi]::CloseHandle($hProc)
    exit 1
}

$funcs = @{}
@("mono_get_root_domain","mono_thread_attach","mono_assembly_open",
  "mono_assembly_get_image","mono_class_from_name",
  "mono_class_get_method_from_name","mono_runtime_invoke") | ForEach-Object {
    $addr = [InjectorApi]::GetProcAddress($localMono, $_)
    $funcs[$_] = $addr
    Write-Host "    $_`: 0x$($addr.ToString('X'))" -ForegroundColor Gray
}

[InjectorApi]::FreeLibrary($localMono)

# Write DLL path (ANSI) into target
$pathBytes = [System.Text.Encoding]::Default.GetBytes($dllPath + "`0")
$pPath = [InjectorApi]::VirtualAllocEx($hProc, [IntPtr]::Zero, $pathBytes.Length, 
    [InjectorApi]::MEM_COMMIT -bor [InjectorApi]::MEM_RESERVE, [InjectorApi]::PAGE_READWRITE)
[InjectorApi]::WriteProcessMemory($hProc, $pPath, $pathBytes, $pathBytes.Length, [ref]0)

# Write namespace/class/method names
$nsBytes = [System.Text.Encoding]::Default.GetBytes("Plugins`0")
$classBytes = [System.Text.Encoding]::Default.GetBytes("Main`0")
$methodBytes = [System.Text.Encoding]::Default.GetBytes("Initialize`0")

$pNs = [InjectorApi]::VirtualAllocEx($hProc, [IntPtr]::Zero, $nsBytes.Length,
    [InjectorApi]::MEM_COMMIT -bor [InjectorApi]::MEM_RESERVE, [InjectorApi]::PAGE_READWRITE)
[InjectorApi]::WriteProcessMemory($hProc, $pNs, $nsBytes, $nsBytes.Length, [ref]0)

$pClass = [InjectorApi]::VirtualAllocEx($hProc, [IntPtr]::Zero, $classBytes.Length,
    [InjectorApi]::MEM_COMMIT -bor [InjectorApi]::MEM_RESERVE, [InjectorApi]::PAGE_READWRITE)
[InjectorApi]::WriteProcessMemory($hProc, $pClass, $classBytes, $classBytes.Length, [ref]0)

$pMethod = [InjectorApi]::VirtualAllocEx($hProc, [IntPtr]::Zero, $methodBytes.Length,
    [InjectorApi]::MEM_COMMIT -bor [InjectorApi]::MEM_RESERVE, [InjectorApi]::PAGE_READWRITE)
[InjectorApi]::WriteProcessMemory($hProc, $pMethod, $methodBytes, $methodBytes.Length, [ref]0)

Write-Host "[+] Memory allocated, writing shellcode..." -ForegroundColor Green

# Build x64 shellcode
$sc = [System.Collections.Generic.List[byte]]::new()

# Prolog
$sc.AddRange([byte[]]@(0x53,0x55,0x56,0x57))  # push rbx,rbp,rsi,rdi
$sc.AddRange([byte[]]@(0x48,0x83,0xEC,0x28))  # sub rsp,0x28

# domain = mono_get_root_domain()
$sc.AddRange([byte[]]@(0x48,0xB8)); $sc.AddRange([BitConverter]::GetBytes([long]$funcs["mono_get_root_domain"]))
$sc.AddRange([byte[]]@(0xFF,0xD0))
$sc.AddRange([byte[]]@(0x48,0x89,0xC3))  # mov rbx,rax

# mono_thread_attach(domain)
$sc.AddRange([byte[]]@(0x48,0x89,0xD9))  # mov rcx,rbx
$sc.AddRange([byte[]]@(0x48,0xB8)); $sc.AddRange([BitConverter]::GetBytes([long]$funcs["mono_thread_attach"]))
$sc.AddRange([byte[]]@(0xFF,0xD0))

# assembly = mono_assembly_open(path, NULL)
$sc.AddRange([byte[]]@(0x48,0xB9)); $sc.AddRange([BitConverter]::GetBytes([long]$pPath))
$sc.AddRange([byte[]]@(0x48,0x31,0xD2))  # xor rdx,rdx (NULL status)
$sc.AddRange([byte[]]@(0x48,0xB8)); $sc.AddRange([BitConverter]::GetBytes([long]$funcs["mono_assembly_open"]))
$sc.AddRange([byte[]]@(0xFF,0xD0))
$sc.AddRange([byte[]]@(0x48,0x89,0xC6))  # mov rsi,rax

# Check assembly != NULL
$sc.AddRange([byte[]]@(0x48,0x85,0xC0))  # test rax,rax
$sc.AddRange([byte[]]@(0x0F,0x84))  # jz fail
$failOffsetPos = $sc.Count; $sc.AddRange([BitConverter]::GetBytes([int]0))  # placeholder

# image = mono_assembly_get_image(assembly)
$sc.AddRange([byte[]]@(0x48,0x89,0xF1))  # mov rcx,rsi
$sc.AddRange([byte[]]@(0x48,0xB8)); $sc.AddRange([BitConverter]::GetBytes([long]$funcs["mono_assembly_get_image"]))
$sc.AddRange([byte[]]@(0xFF,0xD0))
$sc.AddRange([byte[]]@(0x48,0x89,0xC7))  # mov rdi,rax

# klass = mono_class_from_name(image, "Plugins", "Main")
$sc.AddRange([byte[]]@(0x48,0x89,0xF9))  # mov rcx,rdi
$sc.AddRange([byte[]]@(0x48,0xBA)); $sc.AddRange([BitConverter]::GetBytes([long]$pNs))
$sc.AddRange([byte[]]@(0x49,0xB8)); $sc.AddRange([BitConverter]::GetBytes([long]$pClass))
$sc.AddRange([byte[]]@(0x48,0xB8)); $sc.AddRange([BitConverter]::GetBytes([long]$funcs["mono_class_from_name"]))
$sc.AddRange([byte[]]@(0xFF,0xD0))
$sc.AddRange([byte[]]@(0x48,0x89,0xC3))  # mov rbx,rax

# Check klass != NULL
$sc.AddRange([byte[]]@(0x48,0x85,0xC0))
$sc.AddRange([byte[]]@(0x0F,0x84)); $failOffsetPos2 = $sc.Count; $sc.AddRange([BitConverter]::GetBytes([int]0))

# method = mono_class_get_method_from_name(klass, "Initialize", -1)
$sc.AddRange([byte[]]@(0x48,0x89,0xD9))  # mov rcx,rbx
$sc.AddRange([byte[]]@(0x48,0xBA)); $sc.AddRange([BitConverter]::GetBytes([long]$pMethod))
$sc.AddRange([byte[]]@(0x49,0xC7,0xC0,0xFF,0xFF,0xFF,0xFF))  # mov r8,-1
$sc.AddRange([byte[]]@(0x48,0xB8)); $sc.AddRange([BitConverter]::GetBytes([long]$funcs["mono_class_get_method_from_name"]))
$sc.AddRange([byte[]]@(0xFF,0xD0))

# mono_runtime_invoke(method, NULL, NULL, NULL)
$sc.AddRange([byte[]]@(0x48,0x89,0xC1))  # mov rcx,rax
$sc.AddRange([byte[]]@(0x48,0x31,0xD2))  # xor rdx,rdx
$sc.AddRange([byte[]]@(0x4D,0x31,0xC0))  # xor r8,r8
$sc.AddRange([byte[]]@(0x4D,0x31,0xC9))  # xor r9,r9
$sc.AddRange([byte[]]@(0x48,0xB8)); $sc.AddRange([BitConverter]::GetBytes([long]$funcs["mono_runtime_invoke"]))
$sc.AddRange([byte[]]@(0xFF,0xD0))

# Epilog (success path)
$epilogPos = $sc.Count
$sc.AddRange([byte[]]@(0x48,0x83,0xC4,0x28))  # add rsp,0x28
$sc.AddRange([byte[]]@(0x5F,0x5E,0x5D,0x5B))  # pop rdi,rsi,rbp,rbx
$sc.AddRange([byte[]]@(0xC3))  # ret

# Patch fail jumps to epilog
$failOffset = $epilogPos - ($failOffsetPos + 4)
[System.Buffer]::BlockCopy([BitConverter]::GetBytes([int]$failOffset), 0, $sc.ToArray(), $failOffsetPos, 4)
$failOffset2 = $epilogPos - ($failOffsetPos2 + 4)
[System.Buffer]::BlockCopy([BitConverter]::GetBytes([int]$failOffset2), 0, $sc.ToArray(), $failOffsetPos2, 4)

# Write and execute shellcode
$pShellcode = [InjectorApi]::VirtualAllocEx($hProc, [IntPtr]::Zero, $sc.Count,
    [InjectorApi]::MEM_COMMIT -bor [InjectorApi]::MEM_RESERVE, [InjectorApi]::PAGE_EXECUTE_READWRITE)
[InjectorApi]::WriteProcessMemory($hProc, $pShellcode, $sc.ToArray(), $sc.Count, [ref]0)

Write-Host "[+] Executing shellcode..." -ForegroundColor Green

$hThread = [InjectorApi]::CreateRemoteThread($hProc, [IntPtr]::Zero, 0, $pShellcode, [IntPtr]::Zero, 0, [ref]0)

if ($hThread -ne [IntPtr]::Zero) {
    $wait = [InjectorApi]::WaitForSingleObject($hThread, 10000)
    if ($wait -eq 0) {
        Write-Host "[+] Injection successful! Plugin initialized." -ForegroundColor Green
    } else {
        Write-Host "[!] Thread timed out (may still be working)." -ForegroundColor Yellow
    }
    [InjectorApi]::CloseHandle($hThread)
} else {
    Write-Host "[!] Failed to create remote thread. Error: $([Runtime.InteropServices.Marshal]::GetLastWin32Error())" -ForegroundColor Red
}

# Cleanup
[InjectorApi]::VirtualFreeEx($hProc, $pShellcode, 0, 0x8000)
[InjectorApi]::VirtualFreeEx($hProc, $pPath, 0, 0x8000)
[InjectorApi]::VirtualFreeEx($hProc, $pNs, 0, 0x8000)
[InjectorApi]::VirtualFreeEx($hProc, $pClass, 0, 0x8000)
[InjectorApi]::VirtualFreeEx($hProc, $pMethod, 0, 0x8000)
[InjectorApi]::CloseHandle($hProc)

Write-Host ""
Write-Host "[*] Done. Switch to game to see the plugin." -ForegroundColor White
Read-Host "Press Enter to exit"
