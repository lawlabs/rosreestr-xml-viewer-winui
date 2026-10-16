<?xml version = "1.0" encoding = "UTF-8"?>
<xsl:stylesheet version = "1.0" xmlns:xsl = "http://www.w3.org/1999/XSL/Transform">   
   <xsl:template match = "/"> 	
      <html> 
		
		<xsl:call-template name="style_tables"/>
        
		<body> 
            <center><h2>Выписка о переходе прав на объект недвижимости</h2></center> 
			<br/>				

			<xsl:apply-templates select="extract_transfer_rights_property/details_statement/group_top_requisites"/>
            
			<xsl:apply-templates select="extract_transfer_rights_property/base_data"/>
			
			<xsl:apply-templates select="extract_transfer_rights_property/right_records"/>			
								
         </body> 
      </html> 
    </xsl:template>  	
   
    <xsl:template match="group_top_requisites">				
        <center><h3>Реквизиты выписки</h3></center>
		<table title="«Реквизиты выписки»">
			<tr><td>Наименование органа регистрации прав: </td> <td><xsl:value-of select = "organ_registr_rights"/></td></tr>			
			<tr><td>Дата формирования выписки: </td><td><xsl:call-template name="formatDate"><xsl:with-param name="dateTime" select="date_formation"/></xsl:call-template></td></tr>
			<tr><td>Регистрационный номер: </td><td><xsl:value-of select = "registration_number"/></td></tr>
		</table>
    </xsl:template>   
   
    <xsl:template match="base_data">				
        <center><h3>Сведения об объекте недвижимости</h3></center>
		<xsl:choose>
			<xsl:when test="room_record">
				<xsl:apply-templates select="room_record"/>
			</xsl:when>
		</xsl:choose>
    </xsl:template>   	
	
	<xsl:template match="room_record">
		<table>
			<tr><td>Вид объекта недвижимости: </td><td>						
				<xsl:choose>
					<xsl:when test="object/common_data/type/code = 002001001000">
					Земельный участок
					</xsl:when>
					<xsl:when test="object/common_data/type/code = 002001002000">
					Здание
					</xsl:when>
					<xsl:when test="object/common_data/type/code = 002001003000">
					Помещение
					</xsl:when>
					<xsl:when test="object/common_data/type/code = 002001004000">
					Сооружение
					</xsl:when>
					<xsl:when test="object/common_data/type/code = 002001005000">
					Объект незавершённого строительства
					</xsl:when>
					<xsl:when test="object/common_data/type/code = 002001006000">
					Предприятие как имущественный комплекс
					</xsl:when>
					<xsl:when test="object/common_data/type/code = 002001007000">
					Единый недвижимый комплекс
					</xsl:when>
					<xsl:when test="object/common_data/type/code = 002001009000">
					Машино-место
					</xsl:when>
					<xsl:when test="object/common_data/type/code = 002001010000">
					Иной объект недвижимости
					</xsl:when>
				</xsl:choose>
			</td></tr>
			<tr><td>Кадастровый номер: </td> <td><xsl:value-of select = "object/common_data/cad_number"/></td></tr>
			<tr><td>Адрес: </td> <td><xsl:value-of select = "address_room/address/address/readable_address"/></td></tr>
		</table>
	</xsl:template>
   
	<xsl:include href="https://legalcalc.ru/xslt/styles/style_tables.xsl"/>	   
	
	<xsl:include href="https://legalcalc.ru/xslt/right_records.xsl"/>	
  
   
</xsl:stylesheet>