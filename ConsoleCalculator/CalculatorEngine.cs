using System;

namespace ConsoleCalculator
{
    // Typ operacji kalkulatora
    public enum Operation
    {
        None,
        Add,
        Subtract,
        Multiply,
        Divide,
        Power,
        AddPercent,
        SubtractPercent,
        MultiplyPercent,
        DividePercent,
        Sqrt,
        Reciprocal
    }

    // Silnik kalkulatora - przechowuje stan i wykonuje obliczenia
    public class CalculatorEngine
    {
        // Aktualnie wpisywana liczba (string aby przechować wpisywanie, ale konwertujemy do double)
        public string CurrentInput { get; private set; } = string.Empty;

        // Ostatni wynik
        public double? LastResult { get; private set; }

        // Ostatnia operacja
        public Operation LastOperation { get; private set; } = Operation.None;

        // Ostatni prawy argument dla operacji dwuargumentowych
        public double? LastRightOperand { get; private set; }

        // Ostatni lewy argument, przy pierwszym wywołaniu gdy jeszcze nie ma LastResult
        public double? LastLeftOperand { get; private set; }

        // Ostatni argument dla operacji jednoargumentowych
        public double? LastUnaryOperand { get; private set; }

        // Czy użytkownik podał nową liczbę od ostatniej operacji
        public bool HasNewInput { get; private set; }

        // Czy w stanie błędu
        public bool ErrorState { get; private set; }

        public string ErrorMessage { get; private set; } = string.Empty;

        // Wprowadzenie liczby (np. "5" lub "12.34")
        public void InputNumber(string value)
        {
            if (ErrorState) return; // w stanie błędu ignorujemy
            CurrentInput = value;
            HasNewInput = true;
        }

        // Ustawienie operacji dwuargumentowej lub jednoargumentowej
        public void SetOperation(Operation op)
        {
            if (ErrorState) return;
            LastOperation = op;
            // Jeśli użytkownik wpisał liczbę przed ustawieniem operacji, zapamiętaj ją jako lewy operand
            if (HasNewInput && TryGetInputAsDouble(out var left))
            {
                LastLeftOperand = left;
                // nie czyścimy CurrentInput tutaj — użytkownik może dalej edytować
            }
            // Nie wykonujemy od razu operacji - użytkownik może wprowadzić prawy operand
        }

        // Zamienia bieżącą operację dwuargumentową na jej wariant procentowy.
        // Używane przez GUI: użytkownik naciska "%" już PO wpisaniu prawego argumentu,
        // np. 200 + 10 % => operacja Add zmienia się na AddPercent, lewy/prawy operand
        // pozostają bez zmian, więc bufor i powtarzanie (repeat) działają tak samo.
        public void MakePercentOperation()
        {
            if (ErrorState) return;
            switch (LastOperation)
            {
                case Operation.Add: LastOperation = Operation.AddPercent; break;
                case Operation.Subtract: LastOperation = Operation.SubtractPercent; break;
                case Operation.Multiply: LastOperation = Operation.MultiplyPercent; break;
                case Operation.Divide: LastOperation = Operation.DividePercent; break;
                // Jeśli operacja jest już procentowa albo None — nic nie robimy.
            }
        }

        // Oblicz wynik (jak =)
        public double? Calculate()
        {
            if (ErrorState) return null;

            try
            {
                // Jeżeli operacja jest jednoargumentowa
                if (LastOperation == Operation.Sqrt || LastOperation == Operation.Reciprocal)
                {
                    return CalculateUnary();
                }

                // Operacje dwuargumentowe
                return CalculateBinary();
            }
            catch (Exception ex)
            {
                SetError(ex.Message);
                return null;
            }
        }

        private double? CalculateUnary()
        {
            double operand;

            if (HasNewInput && TryGetInputAsDouble(out operand))
            {
                // Użytkownik podał nową liczbę - użyj jej
                LastUnaryOperand = operand;
            }
            else if (LastUnaryOperand.HasValue)
            {
                operand = LastUnaryOperand.Value;
            }
            else if (LastResult.HasValue)
            {
                // fallback - użyj ostatniego wyniku
                operand = LastResult.Value;
                LastUnaryOperand = operand;
            }
            else
            {
                SetError("No operand for unary operation");
                return null;
            }

            double result;

            switch (LastOperation)
            {
                case Operation.Sqrt:
                    if (operand < 0) throw new InvalidOperationException("Error: sqrt of negative number");
                    result = Math.Sqrt(operand);
                    break;
                case Operation.Reciprocal:
                    if (operand == 0) throw new DivideByZeroException("Error: reciprocal of zero");
                    result = 1.0 / operand;
                    break;
                default:
                    throw new InvalidOperationException("Unknown unary operation");
            }

            LastResult = result;
            HasNewInput = false;
            CurrentInput = result.ToString();
            return result;
        }

        private double? CalculateBinary()
        {
            double left;
            double right;

            // Determine right operand
            if (HasNewInput && TryGetInputAsDouble(out right))
            {
                // user provided new right operand
                LastRightOperand = right;
            }
            else if (LastRightOperand.HasValue)
            {
                right = LastRightOperand.Value;
            }
            else
            {
                SetError("No right operand");
                return null;
            }

            // Determine left operand: prefer LastResult, then LastLeftOperand (entered before operation), then default 0
            if (LastResult.HasValue)
            {
                left = LastResult.Value;
            }
            else if (LastLeftOperand.HasValue)
            {
                left = LastLeftOperand.Value;
            }
            else
            {
                left = 0;
            }

            // If LastResult is null but user provided new input as right, we need to find original left from previous CurrentInput before operation selection.
            // However our console UI will set LastResult appropriately when user follows the flow: input number -> set operation -> input number -> equals.

            double result;

            switch (LastOperation)
            {
                case Operation.Add:
                    result = left + right;
                    break;
                case Operation.Subtract:
                    result = left - right;
                    break;
                case Operation.Multiply:
                    result = left * right;
                    break;
                case Operation.Divide:
                    if (right == 0) throw new DivideByZeroException("Error: division by zero");
                    result = left / right;
                    break;
                case Operation.Power:
                    result = Math.Pow(left, right);
                    break;
                case Operation.AddPercent:
                    result = left + (left * right / 100.0);
                    break;
                case Operation.SubtractPercent:
                    result = left - (left * right / 100.0);
                    break;
                case Operation.MultiplyPercent:
                    result = left * (right / 100.0);
                    break;
                case Operation.DividePercent:
                    if (right == 0) throw new DivideByZeroException("Error: division by zero");
                    result = left / (right / 100.0);
                    break;
                default:
                    throw new InvalidOperationException("Unknown binary operation");
            }

            LastResult = result;
            HasNewInput = false;
            CurrentInput = result.ToString();
            return result;
        }

        public double? RepeatLastOperation()
        {
            if (ErrorState) return null;

            if (LastOperation == Operation.None)
            {
                SetError("No operation to repeat");
                return null;
            }

            // For unary operations: use LastUnaryOperand
            if (LastOperation == Operation.Sqrt || LastOperation == Operation.Reciprocal)
            {
                if (!LastUnaryOperand.HasValue)
                {
                    SetError("No unary operand to repeat");
                    return null;
                }

                // Use LastResult as left? For unary we apply op to last result or to lastUnaryOperand? Requirements say to use lastUnaryOperand as argument when repeating.
                double operand = LastUnaryOperand.Value;
                double computedResult;
                switch (LastOperation)
                {
                    case Operation.Sqrt:
                        if (operand < 0) { SetError("Error: sqrt of negative number"); return null; }
                        computedResult = Math.Sqrt(operand);
                        break;
                    case Operation.Reciprocal:
                        if (operand == 0) { SetError("Error: reciprocal of zero"); return null; }
                        computedResult = 1.0 / operand;
                        break;
                    default:
                        SetError("Unknown unary operation"); return null;
                }

                LastResult = computedResult;
                CurrentInput = computedResult.ToString();
                return computedResult;
            }

            // Binary operations: require LastRightOperand
            if (!LastRightOperand.HasValue)
            {
                SetError("No right operand to repeat");
                return null;
            }

            if (!LastResult.HasValue)
            {
                SetError("No last result to use as left operand");
                return null;
            }

            double left = LastResult.Value;
            double right = LastRightOperand.Value;

            double computed;
            try
            {
                switch (LastOperation)
                {
                    case Operation.Add:
                        computed = left + right;
                        break;
                    case Operation.Subtract:
                        computed = left - right;
                        break;
                    case Operation.Multiply:
                        computed = left * right;
                        break;
                    case Operation.Divide:
                        if (right == 0) throw new DivideByZeroException("Error: division by zero");
                        computed = left / right;
                        break;
                    case Operation.Power:
                        computed = Math.Pow(left, right);
                        break;
                    case Operation.AddPercent:
                        computed = left + (left * right / 100.0);
                        break;
                    case Operation.SubtractPercent:
                        computed = left - (left * right / 100.0);
                        break;
                    case Operation.MultiplyPercent:
                        computed = left * (right / 100.0);
                        break;
                    case Operation.DividePercent:
                        if (right == 0) throw new DivideByZeroException("Error: division by zero");
                        computed = left / (right / 100.0);
                        break;
                    default:
                        throw new InvalidOperationException("Unknown binary operation");
                }
            }
            catch (Exception ex)
            {
                SetError(ex.Message);
                return null;
            }

            LastResult = computed;
            CurrentInput = computed.ToString();
            return computed;
        }

        public void Reset()
        {
            CurrentInput = string.Empty;
            LastResult = null;
            LastOperation = Operation.None;
            LastRightOperand = null;
            LastUnaryOperand = null;
            HasNewInput = false;
            ErrorState = false;
            ErrorMessage = string.Empty;
        }

        public string GetDisplayValue()
        {
            if (ErrorState) return ErrorMessage;
            if (!string.IsNullOrEmpty(CurrentInput)) return CurrentInput;
            if (LastResult.HasValue) return LastResult.Value.ToString();
            return "0";
        }

        private bool TryGetInputAsDouble(out double value)
        {
            if (string.IsNullOrEmpty(CurrentInput))
            {
                value = 0;
                return false;
            }

            return double.TryParse(CurrentInput, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out value);
        }

        private void SetError(string message)
        {
            ErrorState = true;
            ErrorMessage = message;
        }
    }
}