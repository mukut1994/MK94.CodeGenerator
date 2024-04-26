export interface PageResult<T>
{
    total: number;
    items: T[];
}
export interface Page
{
    pageId: PageId;
    size: number;
    index: number;
}
export interface PageId extends ValueType
{
}
