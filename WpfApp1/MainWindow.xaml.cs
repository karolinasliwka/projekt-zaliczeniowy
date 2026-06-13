using System.Windows;
using System.Windows.Input;
using ConsoleCalculator; // współdzielony silnik z projektu ConsoleCalculator

namespace WpfApp1
{
    /// <summary>
    /// Logika kalkulatora GUI.
    ///
    /// UWAGA: cała logika obliczeń i bufora operacji znajduje się w klasie
    /// <see cref="CalculatorEngine"/> (projekt ConsoleCalculator). Tutaj jest tylko
    /// warstwa interfejsu: zbieranie wpisywanych cyfr i wywoływanie metod silnika
    /// w odpowiedniej kolejności, tak aby bufor (powtarzanie operacji) działał poprawnie.
    ///
    /// Protokół sterowania silnikiem:
    ///   cyfra            -> dopisz do lokalnego bufora _entry (nie ruszamy silnika)
    ///   operator (+-*/^) -> InputNumber(_entry); [Calculate() jeśli był łańcuch]; SetOperation(op)
    ///   =                -> jeśli wpisano nową liczbę: InputNumber(_entry); Calculate()
    ///                       jeśli NIE: RepeatLastOperation()  (powtórzenie z bufora)
    /// </summary>
    public partial class MainWindow : Window
    {
        // Silnik z całą logiką i buforem
        private readonly CalculatorEngine _engine = new CalculatorEngine();

        // Aktualnie wpisywana liczba (separator dziesiętny przechowujemy jako '.').
        // Pusty string oznacza, że użytkownik nie wpisał jeszcze nowej liczby.
        private string _entry = string.Empty;

        // Czy istnieje "oczekująca" operacja dwuargumentowa (potrzebne do łańcucha np. 5+2+3).
        private bool _hasPendingOp;

        // Tekst pokazywany w małym podglądzie nad wynikiem (np. "7 +").
        private string _expression = string.Empty;

        public MainWindow()
        {
            InitializeComponent();
            UpdateDisplay();
        }

        // ---------- WYŚWIETLANIE ----------

        private void UpdateDisplay()
        {
            ExpressionText.Text = _expression;

            if (_engine.ErrorState)
            {
                // W stanie błędu pokazujemy komunikat. Wyjście tylko przez C (reset).
                ResultText.Text = _engine.ErrorMessage;
                return;
            }

            // Gdy użytkownik aktualnie coś wpisuje, pokazujemy to; w przeciwnym razie wynik z silnika.
            ResultText.Text = _entry.Length > 0
                ? _entry.Replace('.', ',')
                : _engine.GetDisplayValue().Replace('.', ',');
        }

        // ---------- WPROWADZANIE LICZB ----------

        private void Digit_Click(object sender, RoutedEventArgs e)
        {
            if (_engine.ErrorState) return;
            string digit = ((System.Windows.Controls.Button)sender).Content!.ToString()!;
            AppendDigit(digit);
        }

        private void Decimal_Click(object sender, RoutedEventArgs e)
        {
            if (_engine.ErrorState) return;
            // Tylko jeden separator dziesiętny w liczbie.
            if (_entry.Contains('.')) return;
            if (_entry.Length == 0) _entry = "0"; // ".5" -> "0.5"
            _entry += ".";
            UpdateDisplay();
        }

        private void AppendDigit(string digit)
        {
            // Usuwamy zbędne wiodące zero ("0" -> "5", ale "0." zostaje).
            if (_entry == "0") _entry = string.Empty;
            _entry += digit;
            UpdateDisplay();
        }

        // ---------- OPERACJE DWUARGUMENTOWE ----------

        private void Add_Click(object sender, RoutedEventArgs e) => Operator(Operation.Add, "+");
        private void Subtract_Click(object sender, RoutedEventArgs e) => Operator(Operation.Subtract, "−");
        private void Multiply_Click(object sender, RoutedEventArgs e) => Operator(Operation.Multiply, "×");
        private void Divide_Click(object sender, RoutedEventArgs e) => Operator(Operation.Divide, "÷");
        private void Power_Click(object sender, RoutedEventArgs e) => Operator(Operation.Power, "^");

        private void Operator(Operation op, string symbol)
        {
            if (_engine.ErrorState) return;

            if (_entry.Length > 0)
            {
                _engine.InputNumber(_entry);
                // Łańcuch działań: jeśli czeka już operacja, najpierw ją policz (np. 5 + 2 + ...).
                if (_hasPendingOp) _engine.Calculate();
                _entry = string.Empty;
            }

            _engine.SetOperation(op);
            _hasPendingOp = true;

            _expression = $"{_engine.GetDisplayValue().Replace('.', ',')} {symbol}";
            UpdateDisplay();
        }

        // ---------- PROCENT ----------

        private void Percent_Click(object sender, RoutedEventArgs e)
        {
            if (_engine.ErrorState) return;

            if (_entry.Length > 0)
            {
                // Klasyczny scenariusz: 200 + 10 %  ->  zamieniamy operację na procentową i liczymy.
                _engine.InputNumber(_entry);
                _engine.MakePercentOperation();
                _engine.Calculate();
                _entry = string.Empty;
            }
            else
            {
                // "%" bez nowej liczby = powtórzenie z bufora (np. 220 + 10% -> 242).
                _engine.RepeatLastOperation();
            }

            _hasPendingOp = false;
            _expression = string.Empty;
            UpdateDisplay();
        }

        // ---------- OPERACJE JEDNOARGUMENTOWE ----------

        private void Sqrt_Click(object sender, RoutedEventArgs e) => Unary(Operation.Sqrt);
        private void Reciprocal_Click(object sender, RoutedEventArgs e) => Unary(Operation.Reciprocal);

        private void Unary(Operation op)
        {
            if (_engine.ErrorState) return;

            if (_entry.Length > 0)
            {
                _engine.InputNumber(_entry);
                _entry = string.Empty;
            }
            _engine.SetOperation(op);
            _engine.Calculate();

            _hasPendingOp = false;
            _expression = string.Empty;
            UpdateDisplay();
        }

        // ---------- RÓWNA SIĘ / POWTÓRZENIE ----------

        private void Equals_Click(object sender, RoutedEventArgs e)
        {
            if (_engine.ErrorState) return;

            if (_entry.Length > 0)
            {
                // Pierwsze "=": dostarczamy prawy argument i liczymy.
                _engine.InputNumber(_entry);
                _engine.Calculate();
                _entry = string.Empty;
            }
            else
            {
                // Kolejne "=" bez nowej liczby: powtórzenie operacji z bufora
                // (ostatni wynik + zapamiętany operand).
                _engine.RepeatLastOperation();
            }

            _hasPendingOp = false;
            _expression = string.Empty;
            UpdateDisplay();
        }

        // ---------- RESET ----------

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            _engine.Reset();
            _entry = string.Empty;
            _hasPendingOp = false;
            _expression = string.Empty;
            UpdateDisplay();
        }

        // ---------- OBSŁUGA KLAWIATURY ----------

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.D0: case Key.NumPad0: AppendDigitIfOk("0"); break;
                case Key.D1: case Key.NumPad1: AppendDigitIfOk("1"); break;
                case Key.D2: case Key.NumPad2: AppendDigitIfOk("2"); break;
                case Key.D3: case Key.NumPad3: AppendDigitIfOk("3"); break;
                case Key.D4: case Key.NumPad4: AppendDigitIfOk("4"); break;
                case Key.D5: case Key.NumPad5: AppendDigitIfOk("5"); break;
                case Key.D6: case Key.NumPad6: AppendDigitIfOk("6"); break;
                case Key.D7: case Key.NumPad7: AppendDigitIfOk("7"); break;
                case Key.D8: case Key.NumPad8: AppendDigitIfOk("8"); break;
                case Key.D9: case Key.NumPad9: AppendDigitIfOk("9"); break;
                case Key.OemComma: case Key.OemPeriod: case Key.Decimal:
                    Decimal_Click(this, new RoutedEventArgs()); break;
                case Key.Add: Operator(Operation.Add, "+"); break;
                case Key.Subtract: Operator(Operation.Subtract, "−"); break;
                case Key.Multiply: Operator(Operation.Multiply, "×"); break;
                case Key.Divide: Operator(Operation.Divide, "÷"); break;
                case Key.Enter: Equals_Click(this, new RoutedEventArgs()); break;
                case Key.Escape: Clear_Click(this, new RoutedEventArgs()); break;
            }
        }

        private void AppendDigitIfOk(string digit)
        {
            if (_engine.ErrorState) return;
            AppendDigit(digit);
        }
    }
}
