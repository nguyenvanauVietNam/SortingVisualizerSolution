# SortingVisualizerSolution

## Demo GIF / GIF minh họa / デモGIF

<!--
Ví dụ / Example / 例:
![Sorting Visualizer Demo](Resources/demo.gif)
-->

---

## Tiếng Việt

### Giới thiệu

`SortingVisualizerSolution` là ứng dụng Desktop mô phỏng thuật toán sắp xếp theo thời gian thực. Giao diện được xây dựng bằng C# WinForms trên .NET Framework 4.8.1, còn logic sắp xếp được tách sang C++ DLL để xử lý nhanh và gọi lại UI qua callback.

Ứng dụng hỗ trợ 30 thuật toán, đa ngôn ngữ Việt - Anh - Nhật, animation khi đổi chỗ phần tử, lịch sử từng bước, xem mã giả thuật toán, và xuất báo cáo `.txt`.

### Yêu cầu môi trường

- Windows 10/11
- Visual Studio 2022 hoặc Visual Studio 2022 Insiders
- Workload `.NET desktop development`
- Workload `Desktop development with C++`
- .NET Framework Targeting Pack `4.8.1`
- MSVC toolset `v143`
- Windows 10 SDK hoặc Windows 11 SDK

### Cấu trúc chính

- `SortingLogic_CPP`: project C++ DLL chứa engine sắp xếp
- `SortingApp_CS`: project C# WinForms gọi C++ DLL
- `Languages/localization.xml`: dữ liệu đa ngôn ngữ
- `Resources`: icon cờ và ảnh nền
- `bin`: thư mục output sau khi build

### Cách build

Mở [SortingVisualizerSolution.sln](D:/03_study/SortingVisualizerSolution/SortingVisualizerSolution.sln) bằng Visual Studio, chọn `x64`, sau đó build `Debug` hoặc `Release`.

Hoặc build bằng MSBuild:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" "D:\03_study\SortingVisualizerSolution\SortingVisualizerSolution.sln" /p:Configuration=Debug /p:Platform=x64 /m
```

Nếu dùng bản Visual Studio khác, hãy thay đường dẫn `MSBuild.exe` tương ứng.

### Thư mục output

Sau khi build, các file chạy được nằm tại:

- `D:\03_study\SortingVisualizerSolution\bin\Debug`
- `D:\03_study\SortingVisualizerSolution\bin\Release`

Mỗi thư mục gồm `SortingApp_CS.exe`, `SortingLogic_CPP.dll`, file config, `Languages`, và `Resources`.

### Cách sử dụng

1. Chạy `SortingApp_CS.exe` trong `bin\Debug` hoặc `bin\Release`.
2. Nhập dãy số nguyên cách nhau bằng dấu phẩy, ví dụ `9, 4, 7, 1, 3`.
3. Nhấn `Tạo dữ liệu`.
4. Chọn nhóm thuật toán và thuật toán cụ thể.
5. Chỉnh `Độ trễ (ms)` nếu muốn xem chậm hơn.
6. Nhấn `Bắt đầu` để sắp xếp, hoặc `Dừng` để hủy giữa chừng.
7. Xem kết quả, lịch sử từng bước, mã giả thuật toán, hoặc xuất báo cáo TXT.

### Ghi chú kỹ thuật

- Luôn build `x64` để C# app và C++ DLL khớp nền tảng.
- Có thể chỉnh màu và ảnh nền trong [App.config](D:/03_study/SortingVisualizerSolution/SortingApp_CS/App.config).
- Báo cáo TXT được xuất theo ngôn ngữ đang chọn trong ứng dụng.

---

## English

### Overview

`SortingVisualizerSolution` is a real-time desktop sorting visualizer. The UI is built with C# WinForms on .NET Framework 4.8.1, while sorting logic is isolated in an unmanaged C++ DLL for better performance and UI callback support.

The application supports 30 sorting algorithms, Vietnamese - English - Japanese localization, swap animation, step history, algorithm pseudocode viewing, and `.txt` report export.

### Requirements

- Windows 10/11
- Visual Studio 2022 or Visual Studio 2022 Insiders
- `.NET desktop development` workload
- `Desktop development with C++` workload
- .NET Framework Targeting Pack `4.8.1`
- MSVC toolset `v143`
- Windows 10 SDK or Windows 11 SDK

### Main Structure

- `SortingLogic_CPP`: C++ DLL project containing the sorting engine
- `SortingApp_CS`: C# WinForms project that calls the C++ DLL
- `Languages/localization.xml`: localization data
- `Resources`: flag icons and background image
- `bin`: build output folder

### Build

Open [SortingVisualizerSolution.sln](D:/03_study/SortingVisualizerSolution/SortingVisualizerSolution.sln) in Visual Studio, select `x64`, then build `Debug` or `Release`.

Or build with MSBuild:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" "D:\03_study\SortingVisualizerSolution\SortingVisualizerSolution.sln" /p:Configuration=Debug /p:Platform=x64 /m
```

If you use another Visual Studio edition, adjust the `MSBuild.exe` path.

### Output Folder

After building, runnable files are generated in:

- `D:\03_study\SortingVisualizerSolution\bin\Debug`
- `D:\03_study\SortingVisualizerSolution\bin\Release`

Each folder contains `SortingApp_CS.exe`, `SortingLogic_CPP.dll`, config file, `Languages`, and `Resources`.

### Usage

1. Run `SortingApp_CS.exe` from `bin\Debug` or `bin\Release`.
2. Enter comma-separated integers, for example `9, 4, 7, 1, 3`.
3. Click `Create Data`.
4. Select an algorithm group and a specific algorithm.
5. Adjust `Delay (ms)` if you want slower visualization.
6. Click `Start` to sort, or `Stop` to cancel while running.
7. Review the result, step history, pseudocode, or export a TXT report.

### Technical Notes

- Always build as `x64` so the C# app and C++ DLL use the same platform.
- Colors and background image can be changed in [App.config](D:/03_study/SortingVisualizerSolution/SortingApp_CS/App.config).
- TXT reports are exported using the currently selected UI language.

---

## 日本語

### 概要

`SortingVisualizerSolution` は、ソートアルゴリズムをリアルタイムで可視化するデスクトップアプリです。UI は .NET Framework 4.8.1 の C# WinForms で作成され、ソート処理は高性能な C++ DLL に分離されています。

このアプリは 30 種類のソートアルゴリズム、ベトナム語・英語・日本語の多言語表示、交換アニメーション、ステップ履歴、擬似コード表示、`.txt` レポート出力をサポートします。

### 必要環境

- Windows 10/11
- Visual Studio 2022 または Visual Studio 2022 Insiders
- `.NET desktop development` ワークロード
- `Desktop development with C++` ワークロード
- .NET Framework Targeting Pack `4.8.1`
- MSVC toolset `v143`
- Windows 10 SDK または Windows 11 SDK

### 主な構成

- `SortingLogic_CPP`: ソートエンジンを含む C++ DLL プロジェクト
- `SortingApp_CS`: C++ DLL を呼び出す C# WinForms プロジェクト
- `Languages/localization.xml`: 多言語データ
- `Resources`: 国旗アイコンと背景画像
- `bin`: ビルド後の出力フォルダー

### ビルド方法

Visual Studio で [SortingVisualizerSolution.sln](D:/03_study/SortingVisualizerSolution/SortingVisualizerSolution.sln) を開き、`x64` を選択して `Debug` または `Release` をビルドします。

MSBuild を使う場合:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" "D:\03_study\SortingVisualizerSolution\SortingVisualizerSolution.sln" /p:Configuration=Debug /p:Platform=x64 /m
```

別の Visual Studio を使用している場合は、`MSBuild.exe` のパスを環境に合わせて変更してください。

### 出力フォルダー

ビルド後、実行に必要なファイルは以下に生成されます。

- `D:\03_study\SortingVisualizerSolution\bin\Debug`
- `D:\03_study\SortingVisualizerSolution\bin\Release`

各フォルダーには `SortingApp_CS.exe`、`SortingLogic_CPP.dll`、設定ファイル、`Languages`、`Resources` が含まれます。

### 使い方

1. `bin\Debug` または `bin\Release` の `SortingApp_CS.exe` を実行します。
2. `9, 4, 7, 1, 3` のように、カンマ区切りで整数を入力します。
3. `Create Data` をクリックします。
4. アルゴリズムのカテゴリと具体的なアルゴリズムを選択します。
5. 必要に応じて `Delay (ms)` を調整します。
6. `Start` でソートを開始し、実行中に `Stop` で中断できます。
7. 結果、ステップ履歴、擬似コードを確認し、TXT レポートを出力できます。

### 技術メモ

- C# アプリと C++ DLL のプラットフォームを一致させるため、必ず `x64` でビルドしてください。
- 色と背景画像は [App.config](D:/03_study/SortingVisualizerSolution/SortingApp_CS/App.config) で変更できます。
- TXT レポートは、アプリで現在選択されている言語で出力されます。
