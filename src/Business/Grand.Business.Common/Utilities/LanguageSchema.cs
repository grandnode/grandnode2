namespace Grand.Business.Common.Utilities;

public static class LanguageSchema
{
    public const string SchemaXsd = @"
    <xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'>
      <xs:element name='Language'>
        <xs:complexType>
          <xs:sequence>
            <xs:element name='Resource' maxOccurs='unbounded'>
              <xs:complexType>
                <xs:sequence>
                  <xs:element name='Value' type='xs:string' />
                </xs:sequence>
                <xs:attribute name='Name' type='xs:string' use='required' />
                <xs:attribute name='Area' type='xs:string' use='optional' />
              </xs:complexType>
            </xs:element>
          </xs:sequence>
          <xs:attribute name='Name' type='xs:string' use='required' />
        </xs:complexType>
      </xs:element>
    </xs:schema>";
}