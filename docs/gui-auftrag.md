# Auftrag: Erfassungs-GUI für das EÜR-Tool

Stand: 14.10.2026 · Repo `eur-tool` · Branch `main` · Ausgangscommit `01ba39f`

Dieser Auftrag ist vollständig und eigenständig. Die Durchführungssession arbeitet ihn autonom ab, Schritt für Schritt in der Reihenfolge aus Abschnitt 6. Rückfragen an Paul nur bei den Stop-Bedingungen in Abschnitt 9.

---

## 1. Ziel

Paul ist Kleinunternehmer nach §19 UStG. Er soll Einnahmen und Ausgaben eines Jahres in einer WPF-Oberfläche erfassen, ansehen, filtern, bearbeiten und löschen können und Summen sehen (Jahr, Monat, Gewinn). Jede Änderung wird sofort verschlüsselt in die Jahresdatei gespeichert (Autosave).

Getroffene Entscheidungen (nicht neu verhandeln):

| Thema | Entscheidung |
|---|---|
| Umsatzsteuer | Keine. Kein Netto, keine MwSt, kein Brutto. Es gibt genau **einen Betrag** pro Buchung (der tatsächlich gezahlte Betrag). |
| Felder Einnahme | Datum, Betrag, Beschreibung, Belegnummer |
| Felder Ausgabe | Datum, Betrag, **Kategorie**, Beschreibung, Belegnummer |
| Ansicht „Tabelle" | Filter (Typ, Monat), Summen Einnahmen/Ausgaben/Gewinn, Bearbeiten/Löschen, Monatsübersicht |
| Speichern | **Autosave** nach jeder Erfassung, Änderung und Löschung |
| Speicherort | Bleibt wie im Code: `Environment.SpecialFolder.MyDocuments` → `C:\Users\bergp\Documents\eur_tool\eur_data_{Jahr}.csv` |
| Architektur | Code-Behind wie im Bestand, kein MVVM. Masken als UserControls mit Code-Behind. |
| Betragstyp | `decimal` (Euro mit Cent), nicht mehr `int` |

## 2. Abgrenzung: was NICHT gebaut wird

- Keine USt-/Vorsteuer-Logik, keine Netto/Brutto-Umrechnung, keine Prüfung der Kleinunternehmergrenze.
- Keine Summen je Kategorie, keine Zuordnung zu Zeilen der Anlage EÜR, kein ELSTER, kein PDF, kein Druck.
- Keine AfA und kein Anlagenverzeichnis (Wirtschaftsgüter über 800 € werden nicht gesondert behandelt).
- Kein Feld Kunde/Lieferant.
- Keine Belegablage und keine Dateianhänge. Kein Drag&Drop- oder KI-Import: Die Stub-Konstruktoren `Einnahmen(string filepath)` und `Ausgaben(string filepath)` bleiben inhaltlich unverändert stehen.
- `Cryption.cs` wird nicht geändert (Algorithmus und Schlüssel bleiben).
- Der Speicherort wird nicht geändert.
- Der Sidebar-Button „Platzhalter" und `gridPlaceholder` bleiben leer.
- Kein Jahreswechsel innerhalb einer laufenden Sitzung (zurück zur Jahresauswahl gibt es nicht).
- Keine Migration alter Datenzeilen im Netto/MwSt/Brutto-Format (es gibt keine; siehe 4.3).
- Kein MVVM-Umbau, keine NuGet-Pakete, kein Testprojekt, keine Änderung an Fenstergröße, Header oder Jahresauswahl-Layout.
- Die Session startet die App nicht und liest oder schreibt keine Dateien in `Documents\eur_tool\`.

## 3. Bestand (Ausgangslage)

- `eur-tool.csproj`: WPF, `net10.0-windows`, `Nullable` und `ImplicitUsings` aktiv, Namespace `eur_tool`.
- `MainWindow.xaml`: Fenster 1100×600, `WindowStyle=None`, `NoResize`. Zeile 0 ist der Header, Zeile 1 enthält `gridYearSelection` (ComboBox `cmbYear`, Start-Button) und `gridMainFrame` (Sidebar 0.2* mit `btnEinnahmen`, `btnAusgaben`, `btnTabelle`, `btnExport`, `btnPlaceholder` sowie die leeren Grids `gridEinnahmen`, `gridAusgaben`, `gridTabelle`, `gridExport`, `gridPlaceholder` in Spalte 1). Für Inhalte stehen damit rund 880×540 px zur Verfügung.
- `MainWindow.xaml.cs`: `btnConfirmYear_Click` erzeugt `new Controller(this, selectedYear)` und ruft `SetActiveSection(btnEinnahmen)` auf. `SidebarButton_Click` ruft bei Export `controller.ExportData()` auf, sonst `SetActiveSection(button)`. `SetActiveSection` schaltet Visibility und setzt `Tag="Active"`. `hasUnsavedChanges` wird in `btnClose_Click` abgefragt.
- `Controller.cs`: hält `MainWindow mw`, `Datastorage data` und `selectedYear` und bietet nur `ExportData()`.
- `Datastorage.cs`: Schema `Type;Date;Netto;MwSt;Brutto;Category;Comment`, private Listen `einnahmen` und `ausgaben`, `ReadDataFromFile()`, `WriteDataToFile(string)` (privat, nur vom Export genutzt), `CheckLineformat(string[])`, `ExportData()` mit Rückfrage und SaveFileDialog.
- `Transactiontypes/Einnahmen.cs`, `Ausgaben.cs`: `int` netto/mwst/brutto mit 19 % USt, `GetDataString()`. Nur Ausgaben haben `category` und `comment`.
- `Cryption.cs`: `Encrypt` und `Decrypt` je Zeile (XOR mit `Environment.UserName`, Base64).
- `Assets/Styles.xaml`: Style `SidebarButton`. Farben im Bestand: Hintergrund `#CCCCC3`, Sidebar `#E2E2DA`, Buttons `#E5E5DE`, Rahmen `#D0D0C8`, Hover `#DADAD2`, aktiv `#C9C9BE`/`#77776E`, Text `#252520`.

**Bekannte Fehler im Bestand, die dieser Auftrag behebt:**

1. `ReadDataFromFile`: `Cryption.Decrypt(sr.ReadLine())` bekommt nach der letzten Zeile `null`, und `Convert.FromBase64String(null)` wirft eine Exception. Jede vorhandene Datei bringt den Start also zum Absturz, auch die aktuelle `eur_data_2026.csv`, die nur die Kopfzeile enthält.
2. Eine Datei, die neu als 0-Byte-Datei angelegt wurde, scheitert beim nächsten Start am Schema-Check (`null != schema`).
3. Ausgaben verlieren beim Einlesen Kategorie und Kommentar (der Konstruktor bekommt sie nicht übergeben).
4. `ExportData` schreibt **verschlüsselt** (`WriteDataToFile` nutzt `Encrypt`), obwohl der Dialog „unverschlüsselt" ankündigt.
5. Es gibt weder ein öffentliches Hinzufügen/Ändern/Löschen noch ein Speichern in die eigene Datei.
6. Exceptions aus dem `Datastorage`-Konstruktor werden in `btnConfirmYear_Click` nicht abgefangen, die App stürzt ab.

**Stil des Bestands (einhalten):** Deutsche Kommentare (`//` sowie `/// <summary>` an jeder Methode), Properties klein geschrieben (`date`, `category`), Konstruktorparameter mit Unterstrich (`_date`), `#region` in `MainWindow.xaml.cs`, Buttons mit abgerundetem `ControlTemplate` (CornerRadius 5–6).

## 4. Datenmodell und Datenhaltung

### 4.1 Neue Basisklasse `Transactiontypes/Buchung.cs`

```csharp
namespace eur_tool.Transactiontypes
{
    public abstract class Buchung
    {
        public DateTime date { get; protected set; }
        public decimal amount { get; protected set; }
        public string category { get; protected set; } = "";     // bei Einnahmen immer ""
        public string description { get; protected set; } = "";
        public string receiptNo { get; protected set; } = "";

        public abstract string typeCode { get; }  // "1" = Einnahme, "2" = Ausgabe
        public abstract string typeName { get; }  // "Einnahme" / "Ausgabe"

        // Zeile im Klartext vor der Verschlüsselung, Felder siehe 4.2
        public string GetDataString() { ... }
    }
}
```

- `Einnahmen : Buchung` mit dem Konstruktor `Einnahmen(DateTime _date, decimal _amount, string _description, string _receiptNo)`.
- `Ausgaben : Buchung` mit dem Konstruktor `Ausgaben(DateTime _date, decimal _amount, string _category, string _description, string _receiptNo)`.
- `Ausgaben` bekommt `public static readonly string[] Kategorien` in genau dieser Reihenfolge:
  `Wareneinkauf/Material`, `Fremdleistungen`, `Software & Lizenzen`, `Hardware/GWG (bis 800 €)`, `Telefon & Internet`, `Bürobedarf & Porto`, `Fortbildung & Fachliteratur`, `Reisekosten`, `Fahrtkosten`, `Raumkosten/Arbeitszimmer`, `Werbung & Marketing`, `Versicherungen & Beiträge`, `Bank- & Kontogebühren`, `Sonstiges`.
- Die alten Felder `netto`, `mwst`, `brutto` und `comment` entfallen ersatzlos, ebenso der Parameter `isBrutto`.
- Die Stub-Konstruktoren `(string filepath)` bleiben mit ihrem Kommentar stehen. Sie setzen nur Defaultwerte, damit es kompiliert und keine Nullable-Warnung entsteht.
- `Equals` wird **nicht** überschrieben, weil Ändern und Löschen über Referenzgleichheit laufen.

### 4.2 Dateiformat (neu)

- Kopfzeile im Klartext: `Type;Date;Amount;Category;Description;ReceiptNo`
- Jede weitere Zeile ist `Cryption.Encrypt(GetDataString())`, wobei `GetDataString()` genau 6 Felder liefert:
  `{typeCode};{date:yyyy-MM-dd};{amount:0.00};{category};{description};{receiptNo}`
  Datum und Betrag werden mit `CultureInfo.InvariantCulture` formatiert, also z. B. `2;2026-03-12;49.90;Software & Lizenzen;Domain-Verlängerung;RE-2026-014`.
- Textfelder dürfen kein `;` und keinen Zeilenumbruch enthalten. Das stellt die Eingabeprüfung sicher (5.4).

### 4.3 `Datastorage.cs` (Änderungen)

Felder und Konstruktor:
- `schema` = neue Kopfzeile, zusätzlich `legacySchema = "Type;Date;Netto;MwSt;Brutto;Category;Comment"`.
- `public int year { get; }` wird im Konstruktor gesetzt. Der Pfad bleibt `MyDocuments\eur_tool\eur_data_{year}.csv`.

Lesen (`ReadDataFromFile`):
- Datei fehlt: Ordner anlegen, leere Listen, sofort Kopfzeile schreiben (`SaveToFile()`).
- Datei hat 0 Byte **oder** die erste Zeile ist `legacySchema` und danach kommt keine Datenzeile: leere Listen, sofort mit neuer Kopfzeile überschreiben. Das betrifft Pauls aktuelle `eur_data_2026.csv`.
- Erste Zeile ist `legacySchema` und es folgen Datenzeilen: Exception „Datendatei im alten Format mit Daten gefunden … bitte prüfen: {Pfad}".
- Sonst muss die erste Zeile `schema` sein, andernfalls bleibt es bei der bisherigen Schema-Exception.
- Die Schleife wird korrigiert: `while ((raw = sr.ReadLine()) != null)`. Leerzeilen überspringen, erst danach `Decrypt`. Eine `FormatException` aus `Decrypt` wird als Exception mit Zeilennummer weitergegeben.
- `CheckLineformat` wird auf 6 Felder umgestellt und prüft: Typ `1`/`2`, Datum per `DateTime.TryParseExact(…, "yyyy-MM-dd", InvariantCulture, …)`, Jahr des Datums == `year`, Betrag per `decimal.TryParse(…, NumberStyles.Number, InvariantCulture, …)` und > 0. Unbekannte Kategorien sind beim Lesen erlaubt.
- Typ `1` wird `new Einnahmen(...)`, Typ `2` wird `new Ausgaben(...)`, jeweils mit **allen** Feldern.

Schreiben:
- `private void SaveToFile()`: schreibt Kopfzeile und verschlüsselte Zeilen (sortiert nach Datum) in `{dataFilepath}.tmp` und ersetzt danach per `File.Move(tmp, dataFilepath, true)`. So bleibt die Datei auch bei einem Abbruch heil.
- Die alte Methode `WriteDataToFile` entfällt bzw. geht in `SaveToFile` und `WriteExportFile` auf.

Öffentliche API (alle ändernden Methoden speichern sofort, das ist der Autosave):
```csharp
public List<Buchung> GetAll()                               // beide Listen, sortiert nach date (stabil)
public void Add(Buchung entry)                              // in passende Liste, SaveToFile()
public void Replace(Buchung oldEntry, Buchung newEntry)     // gleiche Position ersetzen, SaveToFile()
public void Remove(Buchung entry)                           // entfernen, SaveToFile()
```
Schlägt `SaveToFile()` fehl, macht jede dieser Methoden ihre Änderung an der Liste **rückgängig** und wirft die Exception weiter. Speicher und Datei sind damit immer gleich. `Replace` verlangt, dass alter und neuer Eintrag vom gleichen Typ sind, sonst `ArgumentException`.

Export (`ExportData`, Rückfrage und Dialog bleiben wie im Bestand):
- Neue Methode `private void WriteExportFile(string filepath)`, **unverschlüsselt**, UTF-8 mit BOM (für Excel), Trennzeichen `;`.
- Kopfzeile: `Typ;Datum;Betrag;Kategorie;Beschreibung;Belegnummer`
- Zeilen sortiert nach Datum: `Einnahme`/`Ausgabe`, Datum `dd.MM.yyyy`, Betrag `0.00` mit Kultur `de-DE` (also `49,90`), danach die Textfelder.

### 4.4 `Controller.cs` (Erweiterung)

```csharp
public bool AddEntry(Buchung entry)
public bool ReplaceEntry(Buchung oldEntry, Buchung newEntry)
public bool DeleteEntry(Buchung entry)
public List<Buchung> GetEntries(int typeFilter, int month)    // typeFilter: 0 alle, 1 Einnahmen, 2 Ausgaben; month: 0 = ganzes Jahr, 1..12
public (decimal einnahmen, decimal ausgaben, decimal gewinn) GetSums(int month)   // ignoriert den Typfilter
public List<MonthSum> GetMonthlySums()                        // immer 12 Zeilen, Monate ohne Einträge mit 0
```
- `public record MonthSum(string month, decimal einnahmen, decimal ausgaben, decimal gewinn);` kommt in `Controller.cs`. `month` ist der deutsche Monatsname aus `CultureInfo.GetCultureInfo("de-DE").DateTimeFormat.MonthNames`.
- Die `bool`-Methoden fangen Exceptions aus `Datastorage`, zeigen `MessageBox` „Speichern fehlgeschlagen" (Text plus `ex.Message`, `MessageBoxImage.Error`) und geben `false` zurück. Bei Erfolg geben sie `true` zurück.
- Gewinn = Einnahmen − Ausgaben.

## 5. Ansichten

### 5.1 Allgemein

- `MainWindow.xaml` bekommt am `<Window>` das Attribut `Language="de-DE"`, damit `StringFormat` in Bindings deutsch formatiert (Datum `dd.MM.yyyy`, Beträge `1.234,56`).
- Neue Styles in `Assets/Styles.xaml`, Farben aus dem Bestand: `FormLabel` (TextBlock, FontSize 14), `FormInput` (TextBox, Höhe 30, Rahmen `#D0D0C8`), `ActionButton` (wie `SidebarButton`, aber zentrierter Inhalt, Höhe 36, Breite 140), `ErrorText` (Vordergrund `#B00020`, TextWrapping), `SuccessText` (Vordergrund `#2E7D32`), `HintText` (FontSize 12, `#6B6B63`).
- Betragsanzeige überall `{0:N2} €`, Datumsanzeige `dd.MM.yyyy`.
- Neue Dateien liegen in `Views/` mit dem Namespace `eur_tool.Views` und werden in `MainWindow.xaml` über `xmlns:views="clr-namespace:eur_tool.Views"` eingebunden.

### 5.2 Erfassungsmaske `Views/ErfassungMaske.xaml(.cs)` (UserControl, zweimal verwendet)

Einbindung: `<views:ErfassungMaske x:Name="maskeEinnahmen"/>` in `gridEinnahmen`, `<views:ErfassungMaske x:Name="maskeAusgaben"/>` in `gridAusgaben`.

Öffentliche API:
```csharp
public void Init(Controller controller, bool isAusgabe)   // nach Jahresauswahl aufrufen
public void StartEdit(Buchung entry)                      // Bearbeitungsmodus mit vorbefüllten Feldern
public void CancelEdit()                                  // nur im Bearbeitungsmodus wirksam, löst KEIN Event aus
public bool HasPendingInput { get; }                      // Bearbeitungsmodus aktiv ODER Betrag/Beschreibung/Belegnummer nicht leer
public event EventHandler? EditFinished;                  // nach "Änderung speichern" oder "Abbrechen" im Bearbeitungsmodus
```

Aufbau, von oben nach unten, links bündig mit Rand 30:
1. Überschrift (FontSize 20): „Einnahme erfassen" / „Ausgabe erfassen", im Bearbeitungsmodus „Einnahme bearbeiten" / „Ausgabe bearbeiten".
2. Formular-Grid mit Labelspalte 140 px und Eingabespalte 320 px:

| Feld | Control | Pflicht | Regeln |
|---|---|---|---|
| Datum | `DatePicker` | ja | `DisplayDateStart` = 01.01.{Jahr}, `DisplayDateEnd` = min(31.12.{Jahr}, heute). Vorbelegung: heute, falls im gewählten Jahr, sonst 31.12.{Jahr}. Prüfung: gültiges Datum, Jahr == gewähltes Jahr, nicht in der Zukunft (Meldung: „Datum liegt in der Zukunft. In der EÜR zählt der Zahlungstag."). |
| Betrag | `TextBox` + TextBlock „€" | ja | siehe 5.4. Darunter ein `HintText`: „Gezahlten Betrag erfassen (Kleinunternehmer §19 UStG, keine USt)." |
| Kategorie | `ComboBox` mit `Ausgaben.Kategorien` | ja | **nur bei Ausgaben sichtbar**, sonst Zeile `Collapsed`. Keine Vorauswahl. |
| Beschreibung | `TextBox`, `MaxLength=100` | ja | getrimmt 1–100 Zeichen, kein `;` |
| Belegnummer | `TextBox`, `MaxLength=30` | nein | getrimmt max. 30 Zeichen, kein `;` |

3. Buttonzeile: „Speichern" (`IsDefault=True`, also Enter), im Bearbeitungsmodus beschriftet mit „Änderung speichern". Daneben „Abbrechen" (`IsCancel=True`, also Esc), nur im Bearbeitungsmodus sichtbar.
4. `ErrorText`: listet **alle** Fehler auf einmal, je Zeile einer. Ungültige Controls bekommen einen roten Rahmen (`#B00020`), der beim nächsten Speicherversuch zurückgesetzt wird.
5. `SuccessText`: nach erfolgreichem Speichern „Gespeichert: 12.03.2026 · 49,90 € · Domain-Verlängerung".

Verhalten:
- **Neu speichern:** prüfen, bei Fehlern abbrechen. Sonst `Einnahmen`/`Ausgaben` erzeugen und `controller.AddEntry(...)` aufrufen. Bei `true` Erfolgsmeldung zeigen, Betrag, Beschreibung, Belegnummer und Kategorie leeren, **Datum behalten** (für Serienerfassung) und den Fokus auf Betrag setzen. Bei `false` bleiben alle Eingaben stehen.
- **Bearbeiten:** `StartEdit` befüllt alle Felder aus dem Eintrag. Steht die Kategorie nicht in `Ausgaben.Kategorien` (z. B. aus einer älteren Liste), wird sie temporär als zusätzlicher Eintrag in die ComboBox aufgenommen. „Änderung speichern" prüft, erzeugt ein neues Objekt und ruft `controller.ReplaceEntry(alt, neu)` auf. Bei `true` werden die Felder geleert, die Maske geht in den Neu-Modus zurück und `EditFinished` wird ausgelöst. „Abbrechen" leert ebenfalls, setzt auf Neu zurück und löst `EditFinished` aus.
- Tab-Reihenfolge: Datum → Betrag → Kategorie → Beschreibung → Belegnummer → Speichern. Beim Anzeigen der Maske bekommt Betrag den Fokus.

### 5.3 Tabellenansicht `Views/TabellenAnsicht.xaml(.cs)` (UserControl in `gridTabelle`)

Öffentliche API:
```csharp
public void Init(Controller controller)
public void Refresh()                                   // Daten, Summen und Monatsübersicht neu laden, Filter beibehalten
public event EventHandler<Buchung>? EditRequested;
```

Aufbau: ein `TabControl` mit zwei Tabs.

**Tab „Einträge"**
- Filterzeile: „Typ:" als ComboBox (`Alle`, `Einnahmen`, `Ausgaben`), „Monat:" als ComboBox (`Ganzes Jahr`, `Januar` … `Dezember`). Vorbelegt mit Alle und Ganzes Jahr. Jede Änderung aktualisiert sofort. Die Auswahl bleibt erhalten, solange die App läuft.
- `DataGrid`: `IsReadOnly=True`, `AutoGenerateColumns=False`, `SelectionMode=Single`, `CanUserAddRows=False`, Sortieren per Spaltenkopf erlaubt. Spalten: Datum (`dd.MM.yyyy`), Typ (`typeName`), Betrag (`{0:N2} €`, rechtsbündig), Kategorie, Beschreibung (`Width=*`), Belegnr. Standardsortierung nach Datum aufsteigend.
- Ist die gefilterte Liste leer, erscheint über dem Grid der `HintText` „Keine Einträge für {Zeitraum}."
- Buttons darunter: „Bearbeiten" und „Löschen", nur aktiv, wenn eine Zeile ausgewählt ist. Doppelklick auf eine Zeile wirkt wie Bearbeiten, die Taste Entf wie Löschen.
- **Bearbeiten** löst `EditRequested(eintrag)` aus.
- **Löschen** fragt per `MessageBox` (YesNo, Warning) nach: „Eintrag wirklich löschen?\n\n12.03.2026 · Ausgabe · 49,90 € · Domain-Verlängerung". Bei Ja folgen `controller.DeleteEntry(...)` und danach `Refresh()`.
- Summenleiste unten: „Summen {März 2026 | 2026}: Einnahmen 1.200,00 € · Ausgaben 349,90 € · Gewinn 850,10 € · {n} Einträge". Die Summen hängen nur vom Monatsfilter ab, nicht vom Typfilter (`GetSums(month)`). Ein negativer Gewinn wird rot (`#B00020`) angezeigt, das wird im Code-Behind gesetzt.

**Tab „Monatsübersicht"**
- `DataGrid` (read-only) mit 12 Zeilen aus `GetMonthlySums()`, Spalten Monat, Einnahmen, Ausgaben, Gewinn (Beträge `{0:N2} €`, rechtsbündig).
- Darunter die Gesamtzeile: „Gesamt {Jahr}: Einnahmen … · Ausgaben … · Gewinn …", ein negativer Gewinn rot.

### 5.4 Eingabeprüfung `InputValidation.cs` (statische Klasse im Projektroot)

```csharp
public static bool TryParseAmount(string input, out decimal amount, out string error)
public static string? CheckText(string input, string fieldName, bool required, int maxLength)  // null = ok, sonst Fehlermeldung
```
Regeln für `TryParseAmount`:
- Trimmen, `€` und Leerzeichen entfernen. Leer ergibt „Betrag fehlt."
- Erlaubt sind Ziffern und **höchstens ein** Dezimaltrenner, wahlweise `,` oder `.` (`12`, `12,5`, `12.50`, `1250,00`). Bei mehr als einem Trenner kommt die Meldung „Betrag bitte ohne Tausenderpunkt eingeben, z. B. 1234,56."
- Höchstens 2 Nachkommastellen, Wert > 0 und ≤ 999.999,99. Geparst wird nach dem Normalisieren von `,` zu `.` mit `InvariantCulture`.
- Fehlermeldungen auf Deutsch, z. B. „Betrag muss größer als 0 sein.", „Betrag hat mehr als 2 Nachkommastellen."

`CheckText`: trimmt den Text und meldet „{Feld} fehlt." bzw. „{Feld} ist zu lang (max. {n} Zeichen)." bzw. „{Feld} darf kein Semikolon enthalten."

### 5.5 `MainWindow.xaml(.cs)` (Verdrahtung)

- `btnConfirmYear_Click`: `new Controller(...)` in `try/catch`. Bei einer Exception `MessageBox` „Daten für {Jahr} konnten nicht geladen werden.\n\n{ex.Message}" (Error) zeigen und auf der Jahresauswahl bleiben. Bei Erfolg `maskeEinnahmen.Init(controller, false)`, `maskeAusgaben.Init(controller, true)`, `tabelle.Init(controller)` aufrufen, danach wie bisher `SetActiveSection(btnEinnahmen)`.
- `SetActiveSection`: Wird eine Maske verlassen, `CancelEdit()` auf ihr aufrufen (ohne Rückfrage). Bei Aktivierung von Tabelle `tabelle.Refresh()` aufrufen.
- `tabelle.EditRequested`: Bei `Einnahmen` erst `SetActiveSection(btnEinnahmen)`, dann `maskeEinnahmen.StartEdit(e)`. Bei `Ausgaben` dasselbe mit Ausgaben. Die Reihenfolge ist wichtig, weil `SetActiveSection` Bearbeitungen abbricht.
- `EditFinished` beider Masken → `SetActiveSection(btnTabelle)`.
- `hasUnsavedChanges` wird zur berechneten Property: `maskeEinnahmen.HasPendingInput || maskeAusgaben.HasPendingInput`. Die Rückfrage in `btnClose_Click` bleibt und bekommt den Text „Eingaben in der Erfassungsmaske sind noch nicht gespeichert. Wirklich schließen?"
- Export bleibt wie im Bestand (`controller.ExportData()`).

## 6. Reihenfolge (kleine Schritte, jeder baut fehlerfrei)

Nach **jedem** Schritt gilt:
1. `dotnet build` ausführen. Ergebnis 0 Fehler, keine neuen Warnungen gegenüber dem Stand davor.
2. Prüfen, ob Nachbardateien betroffen sind (Aufrufer, XAML-Namen).
3. Commit nach den Git-Regeln (Abschnitt 8) mit der angegebenen oder einer gleichwertigen Nachricht.

| # | Schritt | Dateien | Commit-Nachricht |
|---|---|---|---|
| 1 | Datenmodell: `Buchung` anlegen, `Einnahmen`/`Ausgaben` umbauen (4.1). `Datastorage` auf das neue Format umstellen: Schema, Lesen mit Lesefehler-Fix, 0-Byte/Legacy-Handling, `CheckLineformat`, `SaveToFile` (4.2, 4.3 Lesen/Schreiben). `ExportData` übergangsweise auf den neuen Writer zeigen lassen. | `Transactiontypes/*`, `Datastorage.cs` | Datenmodell auf Kleinunternehmer ohne Umsatzsteuer umgestellt |
| 2 | `Datastorage`-API `GetAll`/`Add`/`Replace`/`Remove` mit Rollback. Controller-Methoden aus 4.4. `try/catch` beim Laden in `btnConfirmYear_Click`. | `Datastorage.cs`, `Controller.cs`, `MainWindow.xaml.cs` | Speichern und Ändern von Buchungen ergänzt |
| 3 | Export auf `WriteExportFile` umstellen: unverschlüsselt, UTF-8 mit BOM, deutsches Format (4.3 Export). | `Datastorage.cs` | Export als lesbare unverschlüsselte CSV korrigiert |
| 4 | `Language="de-DE"`, neue Styles (5.1), `InputValidation.cs` (5.4). | `MainWindow.xaml`, `Assets/Styles.xaml`, `InputValidation.cs` | Formularstyles und Eingabeprüfung ergänzt |
| 5 | `ErfassungMaske` mit Neu-Modus anlegen, in `gridEinnahmen` einbinden, `Init` verdrahten (5.2 ohne Bearbeiten, 5.5 teilweise). | `Views/ErfassungMaske.*`, `MainWindow.*` | Erfassungsmaske für Einnahmen ergänzt |
| 6 | Kategoriezeile aktivieren und zweite Instanz in `gridAusgaben` einbinden. | `Views/ErfassungMaske.*`, `MainWindow.xaml` | Erfassungsmaske für Ausgaben mit Kategorie ergänzt |
| 7 | `TabellenAnsicht`, Tab „Einträge" mit Filter, Grid, Leerhinweis und Summenleiste. Refresh bei Aktivierung. | `Views/TabellenAnsicht.*`, `MainWindow.*` | Tabellenansicht mit Filter und Summen ergänzt |
| 8 | Bearbeiten (`StartEdit`, `EditRequested`, `EditFinished`, `CancelEdit` in `SetActiveSection`) und Löschen mit Rückfrage. `HasPendingInput` und `hasUnsavedChanges` (5.5). | `Views/*`, `MainWindow.*` | Bearbeiten und Löschen von Einträgen ergänzt |
| 9 | Tab „Monatsübersicht" mit Gesamtzeile. | `Views/TabellenAnsicht.*` | Monatsübersicht in der Tabellenansicht ergänzt |
| 10 | Aufräumen: alle Compilerwarnungen beseitigen (z. B. `Controller? controller` mit Prüfung, Listen direkt initialisieren). `#region`/`#endregion` in `MainWindow.xaml.cs` innerhalb der Klasse sauber setzen. Ungenutzte `using`s in geänderten Dateien entfernen. Abnahme (Abschnitt 7) selbst durchgehen und Ergebnis berichten. | geänderte Dateien | Compilerwarnungen bereinigt |

## 7. Abnahmekriterien

**Build und Code (prüft die Durchführungssession selbst)**
- [ ] `dotnet build` meldet 0 Fehler und **0 Warnungen**.
- [ ] Jeder Schritt aus Abschnitt 6 ist ein eigener Commit, und jeder Commit baut.
- [ ] Keine Spur von Netto, MwSt oder Brutto mehr im Code (`grep -riE "netto|mwst|brutto"` findet nur `legacySchema`).
- [ ] `Cryption.cs`, der Speicherpfad, die Stub-Konstruktoren `(string filepath)`, Header und Jahresauswahl sind unverändert.
- [ ] Keine neuen NuGet-Pakete, keine Änderung an `eur-tool.csproj`.
- [ ] Alle neuen Methoden haben deutsche `/// <summary>`-Kommentare im Bestandsstil.
- [ ] Keine Dateien in `Documents\eur_tool\` angefasst, App nicht gestartet.

**Funktion (manuelle Abnahme durch Paul; die Session listet diese Punkte im Abschlussbericht auf)**
- [ ] Start mit 2026: kein Absturz, auch mit der vorhandenen Datei, die nur die alte Kopfzeile hat. Danach steht in der Datei die neue Kopfzeile.
- [ ] Start mit einem Jahr ohne Datei: Datei wird mit Kopfzeile angelegt, ein zweiter Start funktioniert.
- [ ] Eine kaputte Datei (z. B. Zeile manuell verändert) führt zu einer Fehlermeldung, und die App bleibt auf der Jahresauswahl, statt abzustürzen.
- [ ] Einnahme erfassen: Datum, Betrag `150`, Beschreibung, optional Belegnummer, Enter speichert. Danach Erfolgsmeldung, Felder leer bis auf Datum, Fokus auf Betrag.
- [ ] Ausgabe erfassen: ohne Kategorie kommt eine Fehlermeldung, mit Kategorie wird gespeichert.
- [ ] Betrag `12,5`, `12.50` und `49,90 €` werden akzeptiert. Abgelehnt werden `0`, `-5`, `1.234,56`, `12,345`, `abc` und leer, jeweils mit verständlicher Meldung.
- [ ] Datum außerhalb des Jahres oder in der Zukunft wird abgelehnt. Beschreibung mit `;` wird abgelehnt.
- [ ] Mehrere Fehler werden gleichzeitig angezeigt, die betroffenen Felder sind rot umrandet.
- [ ] App schließen und neu starten: alle Einträge sind wieder da (Autosave). Die Datei ist bis auf die Kopfzeile nicht im Klartext lesbar.
- [ ] Tabelle: Filter Typ und Monat wirken sofort. Die Summenleiste zeigt Einnahmen, Ausgaben und Gewinn für den gewählten Monat bzw. das Jahr, unabhängig vom Typfilter. Ein negativer Gewinn ist rot.
- [ ] Doppelklick oder „Bearbeiten" öffnet die richtige Maske vorbefüllt. „Änderung speichern" führt zurück zur Tabelle mit geändertem Eintrag, „Abbrechen"/Esc zurück ohne Änderung.
- [ ] Ein Sidebar-Wechsel während der Bearbeitung bricht ohne Änderung ab.
- [ ] Löschen fragt nach. Nach Ja ist der Eintrag weg, auch nach einem Neustart.
- [ ] Die Monatsübersicht zeigt 12 Monate inklusive leerer Monate mit 0,00 € und eine Gesamtzeile, deren Werte der Jahressumme im Tab „Einträge" entsprechen.
- [ ] Export: Die Datei öffnet sich in Excel lesbar (Umlaute korrekt, Spalten getrennt, Beträge mit Komma) und ist **unverschlüsselt**.
- [ ] Schließen mit halb ausgefüllter Maske fragt nach, mit leerer Maske nicht.

## 8. Git-Regeln (wörtlich, verbindlich)

- Keine Attribution, nie: kein "Co-Authored-By", kein "Claude", kein "Generated with ...",
  weder in Commits noch in PRs, Branches, Kommentaren oder Dateien, die auf GitHub landen.
- Commit-Nachrichten: nur schlichter Text, unter 12 Wörtern, kein Präfix wie "fix:" oder "(feat)".
  Beispiel: "Erfassungsmaske für Einnahmen ergänzt"
- Commit-Autor bleibt mein Git-User, nichts ändern.
- Vor jedem Commit die Nachricht prüfen; enthält sie Claude/Co-Authored, abbrechen und neu schreiben.

Ergänzend zur Umsetzung:
- Committen nur mit `git commit -m "<Nachricht>"`, ohne Trailer und ohne `--author`, und ohne `git config` zu ändern. Direkt danach mit `git log -1 --format=%B` kontrollieren. Steht dort eine verbotene Zeile, sofort `git commit --amend -m "<saubere Nachricht>"` ausführen (nur solange nicht gepusht).
- Gearbeitet wird direkt auf `main`. Erzwingt die Umgebung einen Branch oder Worktree, bekommt er einen neutralen Namen (z. B. `gui-erfassung`).
- Nur Dateien dieses Auftrags stagen (`git add <pfad>`), kein `git add -A`.
- Pushen nur nach ausdrücklicher Freigabe durch Paul.

## 9. Stop-Bedingungen (dann anhalten und Paul fragen)

- Ein Build-Fehler lässt sich nach zwei sauberen Anläufen nicht beheben.
- Eine Entscheidung wäre nötig, die dieser Auftrag nicht abdeckt und die Datenformat, Speicherort oder Bedienung verändert.
- Ein Schritt würde Dateien außerhalb des Repos oder in `Documents\eur_tool\` berühren.

## 10. Abschlussbericht der Durchführungssession

Kurz und auf Deutsch: Liste der Commits (Hash + Nachricht), Ergebnis von `dotnet build` (Fehler/Warnungen), Abnahme Build/Code abgehakt, Funktions-Checkliste als To-do für Paul und offene Punkte bzw. Abweichungen vom Auftrag mit Begründung.
