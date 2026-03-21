export interface GetPaymentOccurrencesResponseOccurrenceDto {
    paymentId: string;
    paymentSourceId: string;
    payeeId: string;
    amount: any | null;
    currency: string;
    frequency: number;
    occurrenceDate: string;
    startDate: string;
    endDate: string | null;
}
