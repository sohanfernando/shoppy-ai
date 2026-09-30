export const ORDER_STATUSES = ['Confirmed', 'Cancelled'] as const;

export type OrderStatus = (typeof ORDER_STATUSES)[number];

export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export const PRODUCT_CATEGORIES = ['Electronics', 'Accessories'] as const;

export type ProductCategory = (typeof PRODUCT_CATEGORIES)[number];

export interface Product {
  id: number;
  name: string;
  sku: string;
  category: ProductCategory;
  unitPrice: number;
  stock: number;
  isActive: boolean;
  createdAt: string;
}

export interface CreateProductRequest {
  name: string;
  sku: string;
  category: ProductCategory;
  unitPrice: number;
  stock: number;
}

export interface Customer {
  id: number;
  name: string;
  email: string;
  createdAt: string;
}

export interface OrderItem {
  id: number;
  productId: number;
  productName: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface Order {
  id: number;
  customerId: number;
  customerName: string;
  orderDate: string;
  subtotal: number;
  discountAmount: number;
  total: number;
  status: OrderStatus;
  items: OrderItem[];
}

export interface OrderSummary {
  id: number;
  customerName: string;
  orderDate: string;
  total: number;
  status: OrderStatus;
}

export interface OrderQuery {
  status: OrderStatus | null;
  customerId: number | null;
  page: number;
  pageSize: number;
}

// The customer comes from the signed-in account
export interface CreateOrderRequest {
  discountPercent: number;
  items: { productId: number; quantity: number }[];
}

export type UserRole = 'Admin' | 'Customer';

export interface AuthUser {
  id: number;
  fullName: string;
  email: string;
  role: UserRole;
  hasPassword: boolean;
  emailConfirmed: boolean;
  twoFactorEnabled: boolean;
}

export interface LoginResponse {
  // true = the password was correct but a 2FA code is still needed
  requiresTwoFactor: boolean;
  user: AuthUser | null;
}

export interface RegisterResponse {
  message: string;
  canSignIn: boolean;
}

export interface MessageResponse {
  message: string;
}

export interface TwoFactorLoginRequest {
  code: string;
  isRecoveryCode: boolean;
  rememberMe: boolean;
  rememberMachine: boolean;
}

export interface ConfirmEmailRequest {
  userId: number;
  token: string;
}

export interface ResetPasswordRequest {
  userId: number;
  token: string;
  password: string;
  confirmPassword: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

export interface TwoFactorSetup {
  sharedKey: string;
  authenticatorUri: string;
}

export interface RecoveryCodes {
  recoveryCodes: string[];
}

export interface LoginRequest {
  email: string;
  password: string;
  rememberMe: boolean;
  // Which sign-in page this came from; the API rejects the wrong one
  portal: UserRole;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  password: string;
  confirmPassword: string;
  registrationKey: string;
}
export interface CustomerRegisterRequest {
  fullName: string;
  email: string;
  password: string;
  confirmPassword: string;
}

// ---- Reviews ----

export interface Review {
  id: number;
  productId: number;
  productName: string;
  customerId: number;
  customerName: string;
  rating: number;
  comment: string;
  createdAt: string;
}

export interface ProductReviews {
  averageRating: number;
  reviewCount: number;
  canReview: boolean;
  cannotReviewReason: string | null;
  reviews: Review[];
}

export interface CreateReviewRequest {
  productId: number;
  rating: number;
  comment: string;
}

// ---- Notifications ----

export interface AppNotification {
  id: number;
  type: string;
  title: string;
  message: string;
  link: string;
  isRead: boolean;
  createdAt: string;
}

export interface NotificationList {
  items: AppNotification[];
  unreadCount: number;
}

// ---- Customer dashboard ----

export interface MonthlySpendPoint {
  month: string;
  total: number;
  orderCount: number;
}

export interface ProductSpendSlice {
  productId: number;
  productName: string;
  total: number;
  quantity: number;
}

export interface RecentOrderSummary {
  id: number;
  orderDate: string;
  total: number;
  status: OrderStatus;
  itemCount: number;
}

export interface CustomerDashboard {
  totalOrders: number;
  confirmedOrders: number;
  cancelledOrders: number;
  totalPaid: number;
  totalSaved: number;
  averageOrderValue: number;
  itemsOrdered: number;
  lastOrderDate: string | null;
  monthlySpend: MonthlySpendPoint[];
  spendByProduct: ProductSpendSlice[];
  recentOrders: RecentOrderSummary[];
}

// ---- Cart (browser only) ----

export interface CartLine {
  productId: number;
  name: string;
  sku: string;
  unitPrice: number;
  quantity: number;
  stock: number;
}
