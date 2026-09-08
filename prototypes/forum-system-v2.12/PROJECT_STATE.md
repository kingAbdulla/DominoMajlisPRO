# Forum System v2.12 — Registry Recovery & Print Fidelity

## Registry recovery
- Existing v29 data is preserved.
- Report records are normalized on load.
- Missing legacy ReportId, ReportVersionId, VerificationId, ForumId, status/type/version fields are repaired in memory and persisted.
- Invalid/null records are isolated/removed instead of breaking the complete registry.
- Each report row renders independently; one malformed record cannot collapse the entire table.
- Registry shows a health badge and count of repaired legacy records.
- Canonical ActivityId resolution is used for report/activity integrity.

## Print/export fidelity
- Preview, Word export, PDF print preview, and share-file all use one officialDocumentCss() source.
- Forum logo/signature/QR dimensions are explicitly constrained with mm-based A4 sizing.
- Oversized logo/page-splitting defect is prevented by explicit max width/height and print rules.
- Tables use fixed layout, controlled font sizes and break-inside rules.
- A4 page uses 190mm content width inside 10mm page margins.
- Word export embeds the same CSS as the application document.
- PDF print opens a dedicated self-contained print document using the same CSS.
- Share uses a self-contained formatted HTML report file when Web Share file sharing is supported; otherwise it shares the verification link.
- Export/share operations create Audit events.

## Remaining AAA roadmap
- True PDF file generation without browser print dialog when cloud/backend phase begins.
- DOCX-native generation for pixel-stable Word output.
- Repeating print header/footer with real page counters.
- Report Approval Inbox and SLA timers.
- Quarterly, comparison and directorate statistical report templates.
- Final cross-role functional QA matrix.
