export interface PayerGroup {
  id: string;
  userId: string;
  name: string;
  memberPersonIds: string[];
}

export interface CreatePayerGroupRequest {
  name: string;
}

export interface UpdatePayerGroupRequest {
  name: string;
}

export interface SetPayerGroupMembersRequest {
  personIds: string[];
}
