# Member Import Format Guide

This document explains the correct format for importing members from **Excel (.xlsx)** or **CSV (.csv)** files.

## Summary
The system uses **Dynamic Column Search**. This means it will search for the following header names in the first row. The order of columns does not matter, and hidden columns are ignored.

## Required & Recommended Columns

| Field Name | Description | Example Value |
| :--- | :--- | :--- |
| **First name** | Member's first name (Supports Thai) | มานิ |
| **Last name** | Member's last name (Supports Thai) | ใจดี |
| **Phone number** | 10-digit phone number (Primary ID) | 0812345698 |
| **Current points** | Remaining points available for use | 100 |
| **Total points** | Historical lifetime accumulated points | 100 |
| **Member type** | Type of member (e.g., General, VIP) | General |
| **Date of Birth** | Date format: `dd-mm-yyyy` | 07-06-1983 |
| **Status** | Member status (Active / Inactive) | Active |
| **Registered** | Registration Date: `dd-mm-yyyy HH:mm` | 28-03-2026 10:00 |

## Optional Columns
- `Age`, `Gender`, `Address`, `Subdistrict`, `District`, `Province`, `Zip Code`
- `Membership Tier` (Silver, Gold, Platinum)
- `Tags` (e.g., "VIP, Wholesaler")
- `Branch`
- `User Id` (LINE User ID, if importing from LINE OA export)
- `How you know us` (Facebook, LINE, friend)

## Best Practices for Thai Characters (CSV)
1. Use **UTF-8 with BOM** (Byte Order Mark) when saving CSV from Excel to ensure Thai characters display correctly.
2. If using **Excel (.xlsx)**, Thai characters are handled automatically.
3. Keep the header names exactly as shown (English headers are preferred for consistency).

## Dynamic Helper
The importer is "Smart"—it will also recognize these alternatives:
- "Phone" -> "Phone number", "Mobile", "เบอร์โทร"
- "Born" -> "Date of Birth", "วันเกิด"
- "Pts" -> "Current points", "แต้มปัจจุบัน"

---
**Template Location**: [member_import_template.csv](file:///d:/@Project/miniApp2GitVAC/AnalystData/adwportal/Doc/member_import_template.csv)
