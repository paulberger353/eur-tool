# Auftrag: Erfassungs-GUI für das EÜR-Tool

Stand: 14.10.2026 · Repo `eur-tool` · Branch `main` · Ausgangscommit `17338f0` (Code wie `01ba39f`, dazu nur dieser Auftrag)

Dieser Auftrag ist vollständig und eigenständig. Die Durchführungssession arbeitet ihn autonom ab, Schritt für Schritt in der Reihenfolge aus Abschnitt 6. Rückfragen an Paul nur bei den Stop-Bedingungen in Abschnitt 9.

---

## 1. Ziel

Paul ist Kleinunternehmer nach §19 UStG. Er soll Einnahmen und Ausgaben eines Jahres in einer WPF-Oberfläche erfassen, ansehen, filtern, bearbeiten und löschen können und Summen sehen (Jahr, Monat, Gewinn). Jede Änderung wird sofort verschlüsselt in die Jahresdatei gespeichert (Autosave). Bearbeiten und Löschen entfernen nie eine Zeile aus der Datei. Die alte Fassung bleibt als inaktive Zeile erhalten (Historie).

Getroffene Entscheidungen (nicht neu verhandeln):

| Thema | Entscheidung |
|---|---|
| Umsatzsteuer | Keine. Kein Netto, keine MwSt, kein Brutto. Es gibt genau **einen Betrag** pro Buchung (der tatsächlich gezahlte Betrag). |
| Felder Einnahme | Datum, Betrag, Beschreibung, Belegnummer |
| Felder Ausgabe | Datum, Betrag, **Kategorie**, Beschreibung, Belegnummer |
| Ansicht „Tabelle" | Filter (Typ, Monat), Häkchen „Inaktive anzeigen", Summen Einnahmen/Ausgaben/Gewinn, Bearbeiten/Löschen, Monatsübersicht |
| Speichern | **Autosave** nach jeder Erfassung, Änderung und Löschung |
| Historie (Paul, 14.10.2026) | Jede Zeile hat `Id`, `IsActive`, `CreatedAt`, `DeactivatedAt`. **Bearbeiten** = alte Zeile inaktiv + neue aktive Zeile mit **derselben Id**. **Löschen** = Zeile inaktiv, sie bleibt in der Datei. Keine `.bak`-Datei. |
| Inaktive Einträge | Sichtbar nur in der Tabelle über das Häkchen „Inaktive anzeigen", grau, nicht bearbeitbar, nie in Summen. Kein Wiederherstellen. |
| Export | **Nur aktive** Einträge, Spalten wie in 4.3, ohne Id und Zeitstempel. |
| Optik (Paul, 14.10.2026) | *„halte den stil eher neutral in den farben und der optik der sidebar"*, umgesetzt in 5.2. |
| Speicherort | Bleibt wie im Code: `Environment.SpecialFolder.MyDocuments` → `C:\Users\bergp\Documents\eur_tool\eur_data_{Jahr}.csv` |
| Architektur | Code-Behind wie im Bestand, kein MVVM. Masken als UserControls mit Code-Behind. |
| Betragstyp | `decimal` (Euro mit Cent), nicht mehr `int` |

## 2. Abgrenzung: was NICHT gebaut wird

- Keine USt-/Vorsteuer-Logik, keine Netto/Brutto-Umrechnung, keine Prüfung der Kleinunternehmergrenze.
- Keine Summen je Kategorie, keine Zuordnung zu Zeilen der Anlage EÜR, kein ELSTER, kein PDF, kein Druck.
- Keine AfA und kein Anlagenverzeichnis (Wirtschaftsgüter über 800 € werden nicht gesondert behandelt).
- Kein Feld Kunde/Lieferant.
- Keine Belegablage und keine Dateianhänge. Kein Drag&Drop- oder KI-Import: Die Stub-Konstruktoren `Einnahmen(string filepath)` und `Ausgaben(string filepath)` bleiben inhaltlich unverändert stehen.
- Kein Wiederherstellen gelöschter Einträge, kein Bearbeiten oder Löschen inaktiver Zeilen, kein physisches Entfernen von Zeilen, keine `.bak`-Datei, kein Komprimieren der Historie.
- `Cryption.cs` wird nicht geändert (Algorithmus und Schlüssel bleiben).
- Der Speicherort wird nicht geändert.
- Der Sidebar-Button „Platzhalter" und `gridPlaceholder` bleiben leer, ebenso `gridExport`.
- Kein Jahreswechsel innerhalb einer laufenden Sitzung (zurück zur Jahresauswahl gibt es nicht).
- Keine Migration alter Datenzeilen im Netto/MwSt/Brutto-Format (es gibt keine; siehe 4.3).
- Kein Schutz gegen zwei gleichzeitig laufende Instanzen.
- Kein MVVM-Umbau, keine NuGet-Pakete, kein Testprojekt, keine Änderung an Fenstergröße, Header-Layout, Jahresauswahl-Layout, Sidebar-Layout oder `SidebarButton`-Style.
- Die Session startet die App nicht und liest oder schreibt keine Dateien in `Documents\eur_tool\`.

## 3. Bestand (Ausgangslage)

- `eur-tool.csproj`: WPF, `net10.0-windows`, `Nullable` und `ImplicitUsings` aktiv, Namespace `eur_tool`. Gebaut wird mit dem lokal installierten SDK 10.0.401.
- **Build-Ausgangsstand (geprüft am 14.10.2026):** 0 Fehler, 6 Warnungen: CS8618 `einnahmen`/`ausgaben` (`Datastorage.cs`), CS8604 `Decrypt(sr.ReadLine())` (`Datastorage.cs`), CS8618 `controller` (`MainWindow.xaml.cs`), CS8618 `category`/`comment` (`Ausgaben.cs`, Stub-Konstruktor).
- `MainWindow.xaml`: Fenster 1100×600, `WindowStyle=None`, `NoResize`. Zeile 0 ist der Header, Zeile 1 enthält `gridYearSelection` (ComboBox `cmbYear`, Start-Button) und `gridMainFrame` (Sidebar 0.2* mit `btnEinnahmen`, `btnAusgaben`, `btnTabelle`, `btnExport`, `btnPlaceholder` sowie die leeren Grids `gridEinnahmen`, `gridAusgaben`, `gridTabelle`, `gridExport`, `gridPlaceholder` in Spalte 1). Für Inhalte stehen damit rund 880×540 px zur Verfügung.
- `MainWindow.xaml.cs`: `btnConfirmYear_Click` erzeugt `new Controller(this, selectedYear)` und ruft `SetActiveSection(btnEinnahmen)` auf. `SidebarButton_Click` ruft bei Export `controller.ExportData()` auf, sonst `SetActiveSection(button)`. `SetActiveSection` schaltet Visibility und setzt `Tag="Active"`. `hasUnsavedChanges` wird in `btnClose_Click` abgefragt. `#endregion` steht außerhalb der Klasse.
- `Controller.cs`: hält `MainWindow mw`, `Datastorage data` und `selectedYear` und bietet nur `ExportData()`.
- `Datastorage.cs`: Schema `Type;Date;Netto;MwSt;Brutto;Category;Comment`, private Listen `einnahmen` und `ausgaben`, `ReadDataFromFile()`, `WriteDataToFile(string)` (privat, schreibt verschlüsselt, nur vom Export genutzt), `CheckLineformat(string[])`, `ExportData()` mit Rückfrage und SaveFileDialog.
- `Transactiontypes/Einnahmen.cs`, `Ausgaben.cs`: `int` netto/mwst/brutto mit 19 % USt, `GetDataString()`. Nur Ausgaben haben `category` und `comment`.
- `Cryption.cs`: `Encrypt` und `Decrypt` je Zeile (XOR mit `Environment.UserName`, Base64).
- `Assets/Styles.xaml`: Style `SidebarButton`. Farben im Bestand: Hintergrund `#CCCCC3`, Sidebar `#E2E2DA`, Buttons `#E5E5DE`, Rahmen `#D0D0C8`, Hover `#DADAD2`, aktiv `#C9C9BE`/`#77776E`, Text `#252520`.
- Pauls Datendatei `eur_data_2026.csv` ist 44 Byte groß: genau die alte Kopfzeile, ohne Zeilenumbruch, ohne Daten. Andere Jahresdateien gibt es nicht.

**Bekannte Fehler im Bestand, die dieser Auftrag behebt:**

1. `ReadDataFromFile`: `Cryption.Decrypt(sr.ReadLine())` bekommt nach der letzten Zeile `null`, und `Convert.FromBase64String(null)` wirft eine Exception. Jede vorhandene Datei bringt den Start also zum Absturz, auch die aktuelle `eur_data_2026.csv`.
2. Eine Datei, die neu als 0-Byte-Datei angelegt wurde, scheitert beim nächsten Start am Schema-Check (`null != schema`).
3. Ausgaben verlieren beim Einlesen Kategorie und Kommentar (der Konstruktor bekommt sie nicht übergeben).
4. `ExportData` schreibt **verschlüsselt** (`WriteDataToFile` nutzt `Encrypt`), obwohl der Dialog „unverschlüsselt" ankündigt.
5. Es gibt weder ein öffentliches Hinzufügen/Ändern/Löschen noch ein Speichern in die eigene Datei.
6. Exceptions aus dem `Datastorage`-Konstruktor werden in `btnConfirmYear_Click` nicht abgefangen, die App stürzt ab.
7. Der Export schlägt als Dateinamen `eur_data_{Jahr}.csv` vor, also denselben Namen wie die Datendatei. Speichert Paul den Export im Ordner `eur_tool`, überschreibt er seine Daten mit einer Klartextdatei.
8. Alt+F4 schließt das Fenster ohne die Rückfrage aus `btnClose_Click`.

**Stil des Bestands (einhalten):** Deutsche Kommentare (`//` sowie `/// <summary>` an jeder Methode), Properties klein geschrieben (`date`, `category`), Konstruktorparameter mit Unterstrich (`_date`), `#region` in `MainWindow.xaml.cs`, Buttons mit abgerundetem `ControlTemplate` (CornerRadius 5–6).

## 4. Datenmodell und Datenhaltung

### 4.1 Neue Basisklasse `Transactiontypes/Buchung.cs`

```csharp
namespace eur_tool.Transactiontypes
{
    public abstract class Buchung
    {
        // Fachliche Felder (vom Benutzer erfasst)
        public DateTime date { get; protected set; }
        public decimal amount { get; protected set; }
        public string category { get; protected set; } = "";     // bei Einnahmen immer ""
        public string description { get; protected set; } = "";
        public string receiptNo { get; protected set; } = "";

        // Historie (setzt ausschließlich Datastorage)
        public int id { get; private set; }                      // fortlaufend je Jahresdatei, gleich über alle Versionen einer Buchung
        public bool isActive { get; private set; } = true;
        public DateTime createdAt { get; private set; }          // Zeitpunkt, zu dem diese Version entstand
        public DateTime? deactivatedAt { get; private set; }     // null, solange aktiv
        public bool isReplaced { get; private set; }             // nur im Speicher: inaktiv durch Bearbeiten (true) oder Löschen (false)

        public abstract string typeCode { get; }  // "1" = Einnahme, "2" = Ausgabe
        public abstract string typeName { get; }  // "Einnahme" / "Ausgabe"
        public string statusName => isActive ? "Aktiv" : (isReplaced ? "Ersetzt" : "Gelöscht");

        internal void SetHistory(int _id, bool _isActive, DateTime _createdAt, DateTime? _deactivatedAt) { ... }
        internal void Deactivate(DateTime _at, bool _replaced) { ... }
        internal void MarkReplaced() { ... }        // beim Einlesen, siehe 4.3
        internal void UndoDeactivate() { ... }      // nur für den Rollback in Datastorage

        // Zeile im Klartext vor der Verschlüsselung, Felder siehe 4.2
        public string GetDataString() { ... }
    }
}
```

- `Einnahmen : Buchung` mit dem Konstruktor `Einnahmen(DateTime _date, decimal _amount, string _description, string _receiptNo)`.
- `Ausgaben : Buchung` mit dem Konstruktor `Ausgaben(DateTime _date, decimal _amount, string _category, string _description, string _receiptNo)`.
- Die Konstruktoren setzen nur die fachlichen Felder. Id und Zeitstempel vergibt `Datastorage` beim Hinzufügen bzw. Einlesen.
- `Ausgaben` bekommt `public static readonly string[] Kategorien` in genau dieser Reihenfolge:
  `Wareneinkauf/Material`, `Fremdleistungen`, `Software & Lizenzen`, `Hardware/GWG (bis 800 €)`, `Telefon & Internet`, `Bürobedarf & Porto`, `Fortbildung & Fachliteratur`, `Reisekosten`, `Fahrtkosten`, `Raumkosten/Arbeitszimmer`, `Werbung & Marketing`, `Versicherungen & Beiträge`, `Bank- & Kontogebühren`, `Sonstiges`.
- Die alten Felder `netto`, `mwst`, `brutto` und `comment` entfallen ersatzlos, ebenso der Parameter `isBrutto`.
- Die Stub-Konstruktoren `(string filepath)` bleiben mit ihrem Kommentar stehen. Dank der Defaultwerte in `Buchung` kompilieren sie ohne Nullable-Warnung.
- `Equals` wird **nicht** überschrieben, weil Ändern und Löschen über Referenzgleichheit laufen.

### 4.2 Dateiformat (neu)

- Kopfzeile im Klartext: `Id;Type;Date;Amount;Category;Description;ReceiptNo;IsActive;CreatedAt;DeactivatedAt`
- Jede weitere Zeile ist `Cryption.Encrypt(GetDataString())`. `GetDataString()` liefert genau 10 Felder:
  `{id};{typeCode};{date:yyyy-MM-dd};{amount:0.00};{category};{description};{receiptNo};{1|0};{createdAt:yyyy-MM-ddTHH:mm:ss};{deactivatedAt:yyyy-MM-ddTHH:mm:ss oder leer}`
- Alles wird mit `CultureInfo.InvariantCulture` formatiert. Zeitstempel sind lokale Zeit, sekundengenau (Millisekunden abschneiden, damit Speicher und Datei gleich sind).
- Beispiel (Klartext vor der Verschlüsselung):
  ```
  7;2;2026-03-12;49.90;Software & Lizenzen;Domain-Verlängerung;RE-2026-014;0;2026-03-12T10:02:11;2026-03-15T08:40:03
  7;2;2026-03-12;59.90;Software & Lizenzen;Domain-Verlängerung;RE-2026-014;1;2026-03-15T08:40:03;
  8;1;2026-03-20;150.00;;Website Kunde A;;0;2026-03-20T09:00:00;2026-04-01T17:12:45
  ```
  Id 7 wurde am 15.03. von 49,90 auf 59,90 geändert (alte Version „Ersetzt"). Id 8 wurde am 01.04. gelöscht.
- Textfelder dürfen kein `;` und keinen Zeilenumbruch enthalten. Das stellt die Eingabeprüfung sicher (5.5).
- Reihenfolge in der Datei: nach `id`, dann `createdAt`. Die Versionen einer Buchung stehen damit untereinander.

### 4.3 `Datastorage.cs` (Änderungen)

Felder und Konstruktor:
- `schema` = neue Kopfzeile, zusätzlich `legacySchema = "Type;Date;Netto;MwSt;Brutto;Category;Comment"`.
- `public int year { get; }` wird im Konstruktor gesetzt. Der Pfad bleibt `MyDocuments\eur_tool\eur_data_{year}.csv`.
- Die Listen `einnahmen`/`ausgaben` enthalten **alle** Zeilen (aktive und inaktive) und werden direkt bei der Deklaration initialisiert. Dazu kommt `private int nextId`.

Lesen (`ReadDataFromFile`):
- Datei fehlt: Ordner anlegen, leere Listen, sofort Kopfzeile schreiben (`SaveToFile()`).
- Datei hat 0 Byte **oder** die erste Zeile ist `legacySchema` und danach kommt keine Datenzeile: leere Listen, sofort mit neuer Kopfzeile überschreiben. Das betrifft Pauls aktuelle `eur_data_2026.csv`.
- Erste Zeile ist `legacySchema` und es folgen Datenzeilen: Exception „Datendatei im alten Format mit Daten gefunden … bitte prüfen: {Pfad}".
- Sonst muss die erste Zeile `schema` sein, andernfalls bleibt es bei der bisherigen Schema-Exception.
- Die Schleife wird korrigiert: `while ((raw = sr.ReadLine()) != null)`. Leerzeilen überspringen, erst danach `Decrypt`. Eine `FormatException` aus `Decrypt` wird als Exception mit Zeilennummer weitergegeben.
- `CheckLineformat` wird auf 10 Felder umgestellt und prüft: Id ist eine ganze Zahl > 0; Typ `1`/`2`; Datum per `DateTime.TryParseExact(…, "yyyy-MM-dd", InvariantCulture, …)`; Jahr des Datums == `year`; Betrag per `decimal.TryParse(…, NumberStyles.Number, InvariantCulture, …)` und > 0; IsActive `1`/`0`; CreatedAt per `TryParseExact(…, "yyyy-MM-ddTHH:mm:ss", …)`; DeactivatedAt leer genau dann, wenn IsActive `1`, sonst gültiger Zeitstempel ≥ CreatedAt. Unbekannte Kategorien sind beim Lesen erlaubt.
- Typ `1` wird `new Einnahmen(...)`, Typ `2` wird `new Ausgaben(...)`, jeweils mit **allen** fachlichen Feldern, danach `SetHistory(...)`.
- Nach dem Einlesen aller Zeilen wird die Historie geprüft. Je Id gibt es höchstens eine aktive Zeile, und alle Zeilen einer Id haben denselben Typ, sonst Exception „Inkonsistente Historie für Id {id} … bitte prüfen: {Pfad}". Eine inaktive Zeile bekommt `MarkReplaced()`, wenn es eine andere Zeile mit gleicher Id gibt, deren `createdAt` == ihrem `deactivatedAt` ist. `nextId` = höchste Id + 1 (bzw. 1).

Schreiben:
- `private void WriteDataToFile(string filepath)` bleibt der **verschlüsselte** Writer: Kopfzeile plus alle Zeilen (aktiv und inaktiv) in der Reihenfolge aus 4.2.
- `private void SaveToFile()`: `WriteDataToFile($"{dataFilepath}.tmp")`, danach `File.Move(tmp, dataFilepath, true)`. So bleibt die Datei auch bei einem Abbruch heil.

Öffentliche API (alle ändernden Methoden speichern sofort, das ist der Autosave):
```csharp
public List<Buchung> GetAll(bool includeInactive = false)   // sortiert nach date, dann id, dann createdAt (stabil)
public void Add(Buchung entry)                              // id = nextId++, createdAt = jetzt, aktiv; SaveToFile()
public void Replace(Buchung oldEntry, Buchung newEntry)     // alt: Deactivate(jetzt, true); neu: gleiche id, createdAt = dasselbe jetzt; SaveToFile()
public void Remove(Buchung entry)                           // Deactivate(jetzt, false), Zeile bleibt in der Liste; SaveToFile()
```
- „jetzt" wird pro Aufruf **einmal** ermittelt (sekundengenau), damit `deactivatedAt` der alten und `createdAt` der neuen Version gleich sind.
- `Replace` verlangt, dass alter und neuer Eintrag vom gleichen Typ sind, sonst `ArgumentException`. `Replace` und `Remove` auf einem inaktiven Eintrag führen zu einer `InvalidOperationException`.
- Schlägt `SaveToFile()` fehl, macht jede Methode ihre Änderung im Speicher **rückgängig** und wirft die Exception weiter. Bei `Add` heißt das: Eintrag entfernen und `nextId` zurücksetzen. Bei `Replace`: neue Version entfernen und `UndoDeactivate()` auf der alten. Bei `Remove`: `UndoDeactivate()`. Speicher und Datei sind damit immer gleich. Außerhalb dieses Rollbacks wird nie eine Zeile aus den Listen entfernt.

Export (`ExportData`, Rückfrage und Dialog wie im Bestand, mit zwei Korrekturen):
- Vorgeschlagener Dateiname `eur_export_{year}.csv` statt des Namens der Datendatei.
- Zeigt der gewählte Pfad auf die Datendatei (`Path.GetFullPath`, Vergleich ohne Groß/Klein), kommt eine Fehlermeldung „Die Datendatei kann nicht als Exportziel verwendet werden." und es wird nichts geschrieben.
- Neue Methode `private void WriteExportFile(string filepath)`, **unverschlüsselt**, UTF-8 mit BOM (für Excel), Trennzeichen `;`, **nur aktive** Einträge (`GetAll()`).
- Kopfzeile: `Typ;Datum;Betrag;Kategorie;Beschreibung;Belegnummer`
- Zeilen sortiert nach Datum: `Einnahme`/`Ausgabe`, Datum `dd.MM.yyyy`, Betrag `0.00` mit Kultur `de-DE` (also `49,90`), danach die Textfelder.

### 4.4 `Controller.cs` (Erweiterung)

```csharp
public bool AddEntry(Buchung entry)
public bool ReplaceEntry(Buchung oldEntry, Buchung newEntry)
public bool DeleteEntry(Buchung entry)
public List<Buchung> GetEntries(int typeFilter, int month, bool includeInactive)  // typeFilter: 0 alle, 1 Einnahmen, 2 Ausgaben; month: 0 = ganzes Jahr, 1..12
public (decimal einnahmen, decimal ausgaben, decimal gewinn, int anzahl) GetSums(int month)  // nur aktive, ignoriert den Typfilter
public List<MonthSum> GetMonthlySums()                        // nur aktive, immer 12 Zeilen, Monate ohne Einträge mit 0
```
- `public record MonthSum(string month, decimal einnahmen, decimal ausgaben, decimal gewinn);` kommt in `Controller.cs`. `month` ist der deutsche Monatsname aus `CultureInfo.GetCultureInfo("de-DE").DateTimeFormat.MonthNames`.
- Die `bool`-Methoden fangen Exceptions aus `Datastorage`, zeigen `MessageBox` „Speichern fehlgeschlagen" (Text plus `ex.Message`, `MessageBoxImage.Error`) und geben `false` zurück. Bei Erfolg geben sie `true` zurück.
- Gewinn = Einnahmen − Ausgaben. Inaktive Einträge zählen **nie** in Summen, Anzahl oder Monatsübersicht.

## 5. Ansichten

### 5.1 Allgemein

- `MainWindow.xaml` bekommt am `<Window>` die Attribute `Language="de-DE"` (damit `StringFormat` in Bindings deutsch formatiert: Datum `dd.MM.yyyy`, Beträge `1.234,56`) und `Closing="Window_Closing"` (siehe 5.6).
- Betragsanzeige überall `{0:N2} €`, Datumsanzeige `dd.MM.yyyy`, Zeitstempel `dd.MM.yyyy HH:mm`. Texte, die im Code-Behind zusammengesetzt werden (Erfolgsmeldung, Summenleiste, Löschrückfrage), formatieren ausdrücklich mit `CultureInfo.GetCultureInfo("de-DE")`.
- Neue Dateien liegen in `Views/` mit dem Namespace `eur_tool.Views` und werden in `MainWindow.xaml` über `xmlns:views="clr-namespace:eur_tool.Views"` eingebunden.

### 5.2 Optik: neutral, im Stil der Sidebar

Leitlinie: Alles sieht aus wie die Sidebar. Warme Grautöne aus dem Bestand, abgerundete Ecken, schlichte Rahmen, keine Verläufe, keine Schatten, keine Icons. Die blauen WPF-Standardakzente (Auswahl, Hover, Fokus) werden ersetzt. Die einzige Akzentfarbe ist ein gedämpftes Rot für Fehler und negativen Gewinn.

Palette als `SolidColorBrush`-Ressourcen in `Assets/Styles.xaml`. Alle neuen Styles verwenden nur diese Keys. `SidebarButton` bleibt unverändert.

| Key | Farbe | Verwendung |
|---|---|---|
| `BrushBackground` | `#CCCCC3` | Fensterhintergrund (Bestand) |
| `BrushPanel` | `#E2E2DA` | Sidebar (Bestand), Inhaltsflächen der Views |
| `BrushControl` | `#E5E5DE` | Buttons, Tab-Reiter, Spaltenköpfe |
| `BrushInput` | `#F5F5F0` | Eingabefelder, ComboBox, DatePicker, Tabellenzeilen |
| `BrushRowAlt` | `#EEEEE8` | jede zweite Tabellenzeile |
| `BrushBorder` | `#D0D0C8` | Rahmen, Gitterlinien |
| `BrushHover` | `#DADAD2` | Hover |
| `BrushActive` | `#C9C9BE` | ausgewählt: aktiver Tab, Tabellenzeile, ComboBox-Eintrag |
| `BrushActiveBorder` | `#77776E` | Rahmen bei aktiv und Tastaturfokus, Rahmen des Speichern-Buttons |
| `BrushText` | `#252520` | Text |
| `BrushTextMuted` | `#6B6B63` | Hinweistexte |
| `BrushTextInactive` | `#8A8A82` | inaktive Tabellenzeilen, deaktivierte Buttons |
| `BrushError` | `#A33B32` | Fehlertext, Fehlerrahmen, negativer Gewinn |

Styles (alle in `Assets/Styles.xaml`):
- `ContentPanel` (Border): Wurzel jeder View. Hintergrund `BrushPanel`, Rahmen `BrushBorder` 1 px, CornerRadius 6, Margin `0,0,8,8`. Die Fläche sitzt neben der Sidebar wie eine zweite Sidebar-Fläche. Grundschrift der Views: FontSize 14, Vordergrund `BrushText`.
- `FormLabel` (TextBlock): FontSize 14, `BrushText`, vertikal zentriert.
- `FormInput` (TextBox): Höhe 30, Hintergrund `BrushInput`, Rahmen `BrushBorder`, Padding `6,0`, vertikal zentrierter Inhalt. **Eigenes schlankes ControlTemplate** (Border mit CornerRadius 5 + `ScrollViewer x:Name="PART_ContentHost"`), damit der blaue Fokusrahmen entfällt. Trigger `IsKeyboardFocused` → Rahmen `BrushActiveBorder`.
- `FormCombo` (ComboBox): Höhe 30, Farben wie `FormInput`. **Eigenes schlankes ControlTemplate**: ToggleButton über die volle Breite mit Pfeil-`Path` rechts, `ContentPresenter` mit `SelectionBoxItem`, `Popup` mit Border (`BrushInput`, Rahmen `BrushBorder`) und `ItemsPresenter` in einem ScrollViewer. Dazu ein `ItemContainerStyle` für `ComboBoxItem`: Hover `BrushHover`, ausgewählt `BrushActive`. Gilt für alle ComboBoxen der Views (Kategorie, Typ- und Monatsfilter). **Fallback:** Klappt das Template nach zwei sauberen Anläufen nicht, bleibt das Standard-Template mit gesetztem Hintergrund und Rahmen. Das ist kein Stop, gehört aber in den Abschlussbericht.
- `FormDate` (DatePicker): Höhe 30, Hintergrund `BrushInput`, Rahmen `BrushBorder`. Kalender-Popup und inneres Textfeld bleiben Standard.
- `ActionButton` (Button): wie `SidebarButton` (Hintergrund `BrushControl`, Rahmen `BrushBorder`, CornerRadius 6, Hover `BrushHover`), aber zentrierter Inhalt, Höhe 36, Breite 140. Trigger `IsEnabled=False` → Vordergrund `BrushTextInactive`. Der Button „Speichern" bekommt zusätzlich Rahmen `BrushActiveBorder` als dezente Hervorhebung.
- `ViewTabItem` (TabItem) und `ViewTabControl` (TabControl): Reiter wie Sidebar-Buttons mit CornerRadius `6,6,0,0`, Padding `14,6`. Normal `BrushControl`, Hover `BrushHover`, ausgewählt `BrushActive` mit Rahmen `BrushActiveBorder` und SemiBold. Inhaltsfläche des TabControl: Hintergrund `BrushPanel`, Rahmen `BrushBorder`.
- `ViewGrid` (DataGrid): Hintergrund `BrushInput`, `RowBackground` `BrushInput`, `AlternatingRowBackground` `BrushRowAlt`, `GridLinesVisibility=Horizontal`, `HorizontalGridLinesBrush` `BrushBorder`, Rahmen `BrushBorder`, `HeadersVisibility=Column`. Spaltenkopf-Style: Hintergrund `BrushControl`, Rahmen `BrushBorder` (`0,0,1,1`), Padding `8,5`, SemiBold, `BrushText`. Zell-Style: kein Zellrahmen, Padding `8,0` (per Template), Trigger `IsSelected` → Hintergrund `BrushActive`, Vordergrund `BrushText` (kein Blau, auch nicht ohne Fokus). Zeilen-Style: `DataTrigger` auf `isActive` = `False` → Vordergrund `BrushTextInactive`.
- `ErrorText` (TextBlock): Vordergrund `BrushError`, TextWrapping.
- `SuccessText` (TextBlock): Vordergrund `BrushText`, der Text beginnt mit „✓ ". Kein Grün.
- `HintText` (TextBlock): FontSize 12, `BrushTextMuted`, TextWrapping.
- CheckBox und Scrollbars dürfen Standard bleiben.

### 5.3 Erfassungsmaske `Views/ErfassungMaske.xaml(.cs)` (UserControl, zweimal verwendet)

Einbindung: `<views:ErfassungMaske x:Name="maskeEinnahmen"/>` in `gridEinnahmen`, `<views:ErfassungMaske x:Name="maskeAusgaben"/>` in `gridAusgaben`.

Öffentliche API:
```csharp
public void Init(Controller controller, bool isAusgabe)   // nach Jahresauswahl aufrufen
public void StartEdit(Buchung entry)                      // Bearbeitungsmodus mit vorbefüllten Feldern (nur aktive Einträge)
public void CancelEdit()                                  // nur im Bearbeitungsmodus wirksam, löst KEIN Event aus
public bool HasPendingInput { get; }                      // Bearbeitungsmodus aktiv ODER Betrag/Beschreibung/Belegnummer nicht leer
public event EventHandler? EditFinished;                  // nach "Änderung speichern" oder "Abbrechen" im Bearbeitungsmodus
```

Aufbau innerhalb von `ContentPanel`, von oben nach unten, links bündig mit Rand 30:
1. Überschrift (FontSize 20, Normal): „Einnahme erfassen" / „Ausgabe erfassen", im Bearbeitungsmodus „Einnahme bearbeiten" / „Ausgabe bearbeiten".
2. Formular-Grid mit Labelspalte 140 px und Eingabespalte 320 px, Zeilenabstand 10 px:

| Feld | Control | Pflicht | Regeln |
|---|---|---|---|
| Datum | `DatePicker` (`FormDate`) | ja | `DisplayDateStart` = 01.01.{Jahr}, `DisplayDateEnd` = min(31.12.{Jahr}, heute). Vorbelegung: heute, falls im gewählten Jahr, sonst 31.12.{Jahr}. Prüfung: gültiges Datum („Datum fehlt oder ist ungültig."), Jahr == gewähltes Jahr, nicht in der Zukunft (Meldung: „Datum liegt in der Zukunft. In der EÜR zählt der Zahlungstag."). |
| Betrag | `TextBox` (`FormInput`) + TextBlock „€" | ja | siehe 5.5. Darunter ein `HintText`: „Gezahlten Betrag erfassen (Kleinunternehmer §19 UStG, keine USt)." |
| Kategorie | `ComboBox` (`FormCombo`) mit `Ausgaben.Kategorien` | ja | **nur bei Ausgaben sichtbar**, sonst Zeile `Collapsed`. Keine Vorauswahl. Meldung: „Kategorie fehlt." |
| Beschreibung | `TextBox` (`FormInput`), `MaxLength=100` | ja | getrimmt 1–100 Zeichen, kein `;` |
| Belegnummer | `TextBox` (`FormInput`), `MaxLength=30` | nein | getrimmt max. 30 Zeichen, kein `;` |

3. Buttonzeile (`ActionButton`): „Speichern" (`IsDefault=True`, also Enter), im Bearbeitungsmodus beschriftet mit „Änderung speichern". Daneben „Abbrechen" (`IsCancel=True`, also Esc), nur im Bearbeitungsmodus sichtbar.
4. `ErrorText`: listet **alle** Fehler auf einmal, je Zeile einer. Ungültige Controls bekommen einen Rahmen `BrushError`, der beim nächsten Speicherversuch zurückgesetzt wird.
5. `SuccessText`: nach erfolgreichem Speichern „✓ Gespeichert: 12.03.2026 · 49,90 € · Domain-Verlängerung". Er wird beim nächsten Speicherversuch und bei `StartEdit` geleert.

Verhalten:
- **Neu speichern:** prüfen, bei Fehlern abbrechen. Sonst `Einnahmen`/`Ausgaben` erzeugen und `controller.AddEntry(...)` aufrufen. Bei `true` Erfolgsmeldung zeigen, Betrag, Beschreibung, Belegnummer und Kategorie leeren, **Datum behalten** (für Serienerfassung) und den Fokus auf Betrag setzen. Bei `false` bleiben alle Eingaben stehen.
- **Bearbeiten:** `StartEdit` befüllt alle Felder aus dem Eintrag und merkt sich den Originaleintrag. Steht die Kategorie nicht in `Ausgaben.Kategorien` (z. B. aus einer älteren Liste), wird sie temporär als zusätzlicher Eintrag in die ComboBox aufgenommen. „Änderung speichern" prüft zuerst. Ist kein Feld geändert (Datum, Betrag, Kategorie, getrimmte Texte), wirkt es wie „Abbrechen", damit keine leere neue Version entsteht. Sonst wird ein neues Objekt erzeugt und `controller.ReplaceEntry(alt, neu)` aufgerufen. Bei `true` werden die Felder geleert, die Maske geht in den Neu-Modus zurück und `EditFinished` wird ausgelöst. „Abbrechen" leert ebenfalls, setzt auf Neu zurück und löst `EditFinished` aus.
- Tab-Reihenfolge: Datum → Betrag → Kategorie → Beschreibung → Belegnummer → Speichern. Beim Anzeigen der Maske bekommt Betrag den Fokus.

### 5.4 Tabellenansicht `Views/TabellenAnsicht.xaml(.cs)` (UserControl in `gridTabelle`)

Öffentliche API:
```csharp
public void Init(Controller controller)
public void Refresh()                                   // Daten, Summen und Monatsübersicht neu laden, Filter und Häkchen beibehalten
public event EventHandler<Buchung>? EditRequested;
```

Aufbau innerhalb von `ContentPanel`: ein `TabControl` (`ViewTabControl`) mit zwei Tabs.

**Tab „Einträge"**
- Filterzeile: „Typ:" als ComboBox (`Alle`, `Einnahmen`, `Ausgaben`), „Monat:" als ComboBox (`Ganzes Jahr`, `Januar` … `Dezember`), daneben die CheckBox „Inaktive anzeigen". Vorbelegt mit Alle, Ganzes Jahr und ohne Häkchen. Jede Änderung aktualisiert sofort. Die Auswahl bleibt erhalten, solange die App läuft.
- `DataGrid` (`ViewGrid`): `IsReadOnly=True`, `AutoGenerateColumns=False`, `SelectionMode=Single`, `CanUserAddRows=False`, `CanUserDeleteRows=False`, Sortieren per Spaltenkopf erlaubt. Alle Bindings `Mode=OneWay`. Spalten: Datum (`dd.MM.yyyy`, 90), Typ (`typeName`, 80), Betrag (`{0:N2} €`, rechtsbündig, 100), Kategorie (160), Beschreibung (`Width=*`, `MinWidth=150`), Belegnr. (100). Standardsortierung nach Datum aufsteigend.
- **Mit Häkchen** kommen inaktive Zeilen dazu, und vier Spalten werden sichtbar (Spalten-`Visibility` im Code-Behind schalten): Nr. (`id`, 50), Status (`statusName`, 80), Erfasst am (`createdAt`, `dd.MM.yyyy HH:mm`, 125), Deaktiviert am (`deactivatedAt`, 125). Inaktive Zeilen sind grau (Zeilen-Style aus 5.2). Ein waagerechter Scrollbalken ist in diesem Modus in Ordnung.
- Ist die angezeigte Liste leer, erscheint über dem Grid der `HintText` „Keine Einträge für {Zeitraum}."
- Buttons darunter (`ActionButton`): „Bearbeiten" und „Löschen", nur aktiv, wenn eine **aktive** Zeile ausgewählt ist. Doppelklick auf eine aktive Zeile wirkt wie Bearbeiten, die Taste Entf wie Löschen. Bei inaktiven Zeilen und Doppelklick auf den Spaltenkopf passiert nichts.
- **Bearbeiten** löst `EditRequested(eintrag)` aus.
- **Löschen** fragt per `MessageBox` (YesNo, Warning) nach: „Eintrag wirklich löschen?\n\n12.03.2026 · Ausgabe · 49,90 € · Domain-Verlängerung\n\nEr bleibt als „Gelöscht" in der Historie (Häkchen „Inaktive anzeigen")." Bei Ja folgen `controller.DeleteEntry(...)` und danach `Refresh()`.
- Summenleiste unten: „Summen {März 2026 | 2026}: Einnahmen 1.200,00 € · Ausgaben 349,90 € · Gewinn 850,10 € · {n} Einträge". Summen und `n` hängen nur vom Monatsfilter ab, nicht vom Typfilter und nicht vom Häkchen (`GetSums(month)`). Ein negativer Gewinn wird in `BrushError` angezeigt, das wird im Code-Behind gesetzt.

**Tab „Monatsübersicht"**
- `DataGrid` (`ViewGrid`, read-only, ohne Sortieren) mit 12 Zeilen aus `GetMonthlySums()`, Spalten Monat, Einnahmen, Ausgaben, Gewinn (Beträge `{0:N2} €`, rechtsbündig).
- Darunter die Gesamtzeile: „Gesamt {Jahr}: Einnahmen … · Ausgaben … · Gewinn …", ein negativer Gewinn in `BrushError`.

### 5.5 Eingabeprüfung `InputValidation.cs` (statische Klasse im Projektroot)

```csharp
public static bool TryParseAmount(string input, out decimal amount, out string error)
public static string? CheckText(string input, string fieldName, bool required, int maxLength)  // null = ok, sonst Fehlermeldung
```
Regeln für `TryParseAmount`:
- Trimmen, `€` und Leerzeichen entfernen. Leer ergibt „Betrag fehlt."
- Erlaubt sind Ziffern und **höchstens ein** Dezimaltrenner, wahlweise `,` oder `.` (`12`, `12,5`, `12.50`, `1250,00`). Bei mehr als einem Trenner kommt die Meldung „Betrag bitte ohne Tausenderpunkt eingeben, z. B. 1234,56."
- Höchstens 2 Nachkommastellen, Wert > 0 und ≤ 999.999,99. Geparst wird nach dem Normalisieren von `,` zu `.` mit `InvariantCulture`.
- Fehlermeldungen auf Deutsch, z. B. „Betrag muss größer als 0 sein.", „Betrag hat mehr als 2 Nachkommastellen.", „Betrag ist keine Zahl."

`CheckText`: trimmt den Text und meldet „{Feld} fehlt." bzw. „{Feld} ist zu lang (max. {n} Zeichen)." bzw. „{Feld} darf kein Semikolon enthalten."

### 5.6 `MainWindow.xaml(.cs)` (Verdrahtung)

- `btnConfirmYear_Click`: `new Controller(...)` in `try/catch`. Bei einer Exception `MessageBox` „Daten für {Jahr} konnten nicht geladen werden.\n\n{ex.Message}" (Error) zeigen und auf der Jahresauswahl bleiben. Bei Erfolg `maskeEinnahmen.Init(controller, false)`, `maskeAusgaben.Init(controller, true)`, `tabelle.Init(controller)` aufrufen, danach wie bisher `SetActiveSection(btnEinnahmen)`.
- `SetActiveSection`: Wird eine Maske verlassen, `CancelEdit()` auf ihr aufrufen (ohne Rückfrage). Bei Aktivierung von Tabelle `tabelle.Refresh()` aufrufen.
- `tabelle.EditRequested`: Bei `Einnahmen` erst `SetActiveSection(btnEinnahmen)`, dann `maskeEinnahmen.StartEdit(e)`. Bei `Ausgaben` dasselbe mit Ausgaben. Die Reihenfolge ist wichtig, weil `SetActiveSection` Bearbeitungen abbricht.
- `EditFinished` beider Masken → `SetActiveSection(btnTabelle)`.
- `hasUnsavedChanges` wird zur berechneten Property: `maskeEinnahmen.HasPendingInput || maskeAusgaben.HasPendingInput`.
- Die Rückfrage zieht von `btnClose_Click` nach `Window_Closing` (Event `Closing`, `e.Cancel = true` bei Nein), damit sie auch bei Alt+F4 greift. Der Text lautet „Eingaben in der Erfassungsmaske sind noch nicht gespeichert. Wirklich schließen?". `btnClose_Click` ruft nur noch `Close()` auf.
- Export bleibt wie im Bestand (`controller.ExportData()`).

### 5.7 Laufzeitfallen, die der Build nicht findet (die App wird nicht gestartet, also vorbeugend einbauen)

1. **StaticResource in UserControls:** Die Views werden erzeugt, bevor sie im Baum von `MainWindow` hängen. Ressourcen aus `MainWindow.Resources` findet `StaticResource` dort nicht, und es gibt eine `XamlParseException` beim Start. Deshalb bindet jede View `Assets/Styles.xaml` in ihren eigenen `UserControl.Resources` per `MergedDictionaries` ein (`Source="/Assets/Styles.xaml"`).
2. **Events vor `Init`:** `SelectionChanged` und `Checked` feuern schon in `InitializeComponent`, wenn die XAML eine Vorauswahl setzt. Jeder Handler beginnt deshalb mit `if (controller == null) return;`.
3. **Bindings auf Properties ohne öffentlichen Setter:** immer `Mode=OneWay`.
4. **Doppelklick im DataGrid:** nur auswerten, wenn er auf einer `DataGridRow` landet (`ItemsControl.ContainerFromElement`), nicht auf dem Spaltenkopf.
5. **Entf im DataGrid:** über `PreviewKeyDown` abfangen und `e.Handled = true` setzen.
6. **Fokus beim Anzeigen der Maske:** `IsVisibleChanged` plus `Dispatcher.BeginInvoke(..., DispatcherPriority.Input)`, ein direktes `Focus()` greift bei einer gerade sichtbar gewordenen Maske nicht.
7. **Spaltenformat:** `StringFormat` in XAML als `StringFormat={}{0:N2} €` schreiben.

## 6. Reihenfolge (kleine Schritte, jeder baut fehlerfrei)

**Vor Schritt 1:** `dotnet --list-sdks` muss ein 10.x-SDK zeigen. `dotnet build` muss den Ausgangsstand aus Abschnitt 3 ergeben (0 Fehler, 6 Warnungen). Ist `docs/gui-auftrag.md` geändert und nicht committet, wird er allein committet: „GUI-Auftrag um Historie und neutrale Optik ergänzt".

Nach **jedem** Schritt gilt:
1. `dotnet build` ausführen. Ergebnis 0 Fehler, keine neuen Warnungen gegenüber dem Stand davor.
2. Prüfen, ob Nachbardateien betroffen sind (Aufrufer, XAML-Namen, Style-Keys).
3. Commit nach den Git-Regeln (Abschnitt 8) mit der angegebenen oder einer gleichwertigen Nachricht.

| # | Schritt | Dateien | Commit-Nachricht |
|---|---|---|---|
| 1 | Datenmodell: `Buchung` mit Historienfeldern anlegen, `Einnahmen`/`Ausgaben` umbauen (4.1). `Datastorage` auf das neue Format umstellen: Schema, Lesen mit Lesefehler-Fix, 0-Byte/Legacy-Handling, `CheckLineformat`, Historienprüfung, `WriteDataToFile` im neuen Format, `SaveToFile` (4.2, 4.3 Lesen/Schreiben). `ExportData` bleibt vorerst unverändert auf `WriteDataToFile`. | `Transactiontypes/*`, `Datastorage.cs` | Datenmodell ohne Umsatzsteuer mit Historie umgestellt |
| 2 | `Datastorage`-API `GetAll`/`Add`/`Replace`/`Remove` mit Historie und Rollback. Controller-Methoden aus 4.4. `try/catch` beim Laden in `btnConfirmYear_Click`. | `Datastorage.cs`, `Controller.cs`, `MainWindow.xaml.cs` | Speichern, Ändern und Deaktivieren von Buchungen ergänzt |
| 3 | Export auf `WriteExportFile` umstellen: unverschlüsselt, UTF-8 mit BOM, nur aktive, deutsches Format, neuer Dateivorschlag, Schutz der Datendatei (4.3 Export). | `Datastorage.cs` | Export als lesbare unverschlüsselte CSV korrigiert |
| 4 | `Language="de-DE"`, Palette und alle Styles aus 5.2, `InputValidation.cs` (5.5). | `MainWindow.xaml`, `Assets/Styles.xaml`, `InputValidation.cs` | Neutrale Styles und Eingabeprüfung ergänzt |
| 5 | `ErfassungMaske` mit Neu-Modus anlegen, in `gridEinnahmen` einbinden, `Init` verdrahten (5.3 ohne Bearbeiten, 5.6 teilweise, 5.7). | `Views/ErfassungMaske.*`, `MainWindow.*` | Erfassungsmaske für Einnahmen ergänzt |
| 6 | Kategoriezeile aktivieren und zweite Instanz in `gridAusgaben` einbinden. | `Views/ErfassungMaske.*`, `MainWindow.xaml` | Erfassungsmaske für Ausgaben mit Kategorie ergänzt |
| 7 | `TabellenAnsicht`, Tab „Einträge" mit Filter, Grid, Leerhinweis und Summenleiste. Refresh bei Aktivierung. | `Views/TabellenAnsicht.*`, `MainWindow.*` | Tabellenansicht mit Filter und Summen ergänzt |
| 8 | Bearbeiten (`StartEdit`, `EditRequested`, `EditFinished`, `CancelEdit` in `SetActiveSection`, „keine Änderung"-Fall) und Löschen mit Rückfrage. `HasPendingInput`, `hasUnsavedChanges`, `Window_Closing` (5.6). | `Views/*`, `MainWindow.*` | Bearbeiten und Löschen von Einträgen ergänzt |
| 9 | Häkchen „Inaktive anzeigen" mit Zusatzspalten, grauen Zeilen und gesperrten Aktionen (5.4). | `Views/TabellenAnsicht.*` | Anzeige inaktiver Einträge in der Tabelle ergänzt |
| 10 | Tab „Monatsübersicht" mit Gesamtzeile. | `Views/TabellenAnsicht.*` | Monatsübersicht in der Tabellenansicht ergänzt |
| 11 | Aufräumen: alle Compilerwarnungen beseitigen (z. B. `Controller? controller` mit Prüfung). `#region`/`#endregion` in `MainWindow.xaml.cs` innerhalb der Klasse sauber setzen. Ungenutzte `using`s in geänderten Dateien entfernen. Abnahme (Abschnitt 7) selbst durchgehen und Ergebnis berichten. | geänderte Dateien | Compilerwarnungen bereinigt |

## 7. Abnahmekriterien

**Build und Code (prüft die Durchführungssession selbst)**
- [ ] `dotnet build` meldet 0 Fehler und **0 Warnungen**.
- [ ] Jeder Schritt aus Abschnitt 6 ist ein eigener Commit, und jeder Commit baut.
- [ ] Keine Spur von Netto, MwSt oder Brutto mehr im Code: `grep -riE "netto|mwst|brutto" --include=*.cs --include=*.xaml .` findet nur `legacySchema`.
- [ ] `Datastorage` entfernt außer beim Rollback von `Add`/`Replace` nie einen Eintrag aus `einnahmen`/`ausgaben`.
- [ ] Neue Styles verwenden nur die Brush-Keys aus 5.2, in `Views/*.xaml` stehen keine Hex-Farben.
- [ ] `Cryption.cs`, der Speicherpfad, die Stub-Konstruktoren `(string filepath)`, Header, Jahresauswahl, Sidebar und `SidebarButton` sind unverändert.
- [ ] Keine neuen NuGet-Pakete, keine Änderung an `eur-tool.csproj`.
- [ ] Alle neuen Methoden haben deutsche `/// <summary>`-Kommentare im Bestandsstil.
- [ ] Keine Dateien in `Documents\eur_tool\` angefasst, App nicht gestartet.

**Funktion (manuelle Abnahme durch Paul; die Session listet diese Punkte im Abschlussbericht auf)**
- [ ] Start mit 2026: kein Absturz, auch mit der vorhandenen Datei, die nur die alte Kopfzeile hat. Danach steht in der Datei die neue Kopfzeile.
- [ ] Start mit einem Jahr ohne Datei (2025): Datei wird mit Kopfzeile angelegt, ein zweiter Start funktioniert.
- [ ] Eine kaputte Datei (z. B. Zeile manuell verändert) führt zu einer Fehlermeldung, und die App bleibt auf der Jahresauswahl, statt abzustürzen.
- [ ] Einnahme erfassen: Datum, Betrag `150`, Beschreibung, optional Belegnummer, Enter speichert. Danach Erfolgsmeldung, Felder leer bis auf Datum, Fokus auf Betrag.
- [ ] Ausgabe erfassen: ohne Kategorie kommt eine Fehlermeldung, mit Kategorie wird gespeichert.
- [ ] Betrag `12,5`, `12.50` und `49,90 €` werden akzeptiert. Abgelehnt werden `0`, `-5`, `1.234,56`, `12,345`, `abc` und leer, jeweils mit verständlicher Meldung.
- [ ] Datum außerhalb des Jahres oder in der Zukunft wird abgelehnt. Beschreibung mit `;` wird abgelehnt.
- [ ] Mehrere Fehler werden gleichzeitig angezeigt, die betroffenen Felder sind rot umrandet.
- [ ] App schließen und neu starten: alle Einträge sind wieder da (Autosave). Die Datei ist bis auf die Kopfzeile nicht im Klartext lesbar.
- [ ] Tabelle: Filter Typ und Monat wirken sofort. Die Summenleiste zeigt Einnahmen, Ausgaben, Gewinn und Anzahl für den gewählten Monat bzw. das Jahr, unabhängig vom Typfilter und vom Häkchen. Ein negativer Gewinn ist rot.
- [ ] Doppelklick oder „Bearbeiten" öffnet die richtige Maske vorbefüllt. „Änderung speichern" führt zurück zur Tabelle mit geändertem Eintrag, „Abbrechen"/Esc zurück ohne Änderung. „Änderung speichern" ohne geändertes Feld erzeugt keine neue Version.
- [ ] Ein Sidebar-Wechsel während der Bearbeitung bricht ohne Änderung ab.
- [ ] Löschen fragt nach. Nach Ja ist der Eintrag aus Liste, Summen, Monatsübersicht und Export verschwunden, auch nach einem Neustart.
- [ ] Häkchen „Inaktive anzeigen": Die gelöschte Zeile erscheint grau mit Status „Gelöscht", die alte Fassung einer Bearbeitung grau mit Status „Ersetzt" und derselben Nr. wie die neue, mit Zeitstempeln. Bearbeiten/Löschen sind bei inaktiven Zeilen gesperrt, Doppelklick und Entf wirken nicht.
- [ ] Die Monatsübersicht zeigt 12 Monate inklusive leerer Monate mit 0,00 € und eine Gesamtzeile, deren Werte der Jahressumme im Tab „Einträge" entsprechen.
- [ ] Export: Vorschlag `eur_export_{Jahr}.csv`. Die Datei öffnet sich in Excel lesbar (Umlaute korrekt, Spalten getrennt, Beträge mit Komma), ist **unverschlüsselt** und enthält nur aktive Einträge. Wird die Datendatei als Ziel gewählt, kommt eine Fehlermeldung.
- [ ] Schließen mit halb ausgefüllter Maske fragt nach, mit leerer Maske nicht, auch bei Alt+F4.
- [ ] Optik: Inhaltsflächen im Ton der Sidebar. Kein WPF-Blau in Tabelle, Tabs, Eingabefeldern und ComboBoxen (außer beim Fallback aus 5.2, laut Abschlussbericht).

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
- Pushen nur nach ausdrücklicher Freigabe durch Paul. Die globale Regel „Commit ⇒ sofort Push" aus Pauls `~/.claude/CLAUDE.md` gilt nur für das second-brain, nicht für dieses Repo.

## 9. Stop-Bedingungen (dann anhalten und Paul fragen)

- Kein .NET-10-SDK vorhanden, oder der Ausgangsbuild weicht von Abschnitt 3 ab.
- Ein Build-Fehler lässt sich nach zwei sauberen Anläufen nicht beheben.
- Eine Entscheidung wäre nötig, die dieser Auftrag nicht abdeckt und die Datenformat, Historie, Speicherort oder Bedienung verändert.
- Ein Schritt würde Dateien außerhalb des Repos oder in `Documents\eur_tool\` berühren.

## 10. Abschlussbericht der Durchführungssession

Kurz und auf Deutsch: Liste der Commits (Hash + Nachricht), Ergebnis von `dotnet build` (Fehler/Warnungen), Abnahme Build/Code abgehakt, Funktions-Checkliste als To-do für Paul und offene Punkte bzw. Abweichungen vom Auftrag mit Begründung (insbesondere, ob der ComboBox-Fallback aus 5.2 gegriffen hat).
