import os, json
results_dir = "shared/results"
print(f"| {'Transaction ID':<15} | {'Final Status':<12} | {'Amount':<10} | {'Risk/Reject Reason'}")
print("-" * 80)
files = os.listdir(results_dir)
for f in sorted(files):
    if not f.endswith(".json"): continue
    path = os.path.join(results_dir, f)
    try:
        with open(path, "r") as file:
            data = json.load(file)
            tx = data.get("data", {})
            tx_id = tx.get("transaction_id", "Unknown")
            status = tx.get("status", "unknown")
            amount = tx.get("amount", "unknown")
            ext = tx.get("ExtensionData", {})
            reason = ext.get("reject_reason", "")
            if not reason:
                risk = ext.get("risk_score", "")
                reason = f"Risk Score: {risk}" if risk else ""
            print(f"| {tx_id:<15} | {status:<12} | {amount:<10} | {reason}")
    except Exception as e:
        print(f"Error reading {f}: {e}")
