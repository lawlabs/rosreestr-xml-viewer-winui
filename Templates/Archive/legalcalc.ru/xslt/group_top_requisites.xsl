<?xml version = "1.0" encoding = "UTF-8"?>
<xsl:stylesheet version = "1.0" xmlns:xsl = "http://www.w3.org/1999/XSL/Transform">   

	<xsl:template match="group_top_requisites">				
        <center><h3>Реквизиты выписки</h3></center>
		<table title="«Реквизиты выписки»">
			<tr><td>Наименование органа регистрации прав: </td> <td><xsl:value-of select = "organ_registr_rights"/></td></tr>			
			<tr><td>Дата формирования выписки: </td><td><xsl:call-template name="formatDate"><xsl:with-param name="dateTime" select="date_formation"/></xsl:call-template></td></tr>
			<tr><td>Регистрационный номер: </td><td><xsl:value-of select = "registration_number"/></td></tr>
		</table>
    </xsl:template> 
	
</xsl:stylesheet>