<?xml version = "1.0" encoding = "UTF-8"?>
<xsl:stylesheet version = "1.0" xmlns:xsl = "http://www.w3.org/1999/XSL/Transform">   

	<xsl:include href="https://legalcalc.ru/xslt/format_datetime.xsl"/>
	
	<xsl:template match="right_records">		
		<br/><br/><br/>
		<center><h3>Cведения о правах и правообладателях</h3></center>
		<xsl:for-each select="right_record">
			<br/>
			<table>
				<tr>
					<td>Дата регистрации права: </td>
					<td>					    
						<xsl:call-template name="formatDateTime"><xsl:with-param name="dateTime" select="record_info/registration_date"/></xsl:call-template>						
					</td>
				</tr>
				<tr>
					<td>Дата снятия с учета/регистрации (перехода/ прекращения права): </td>					
					<td><xsl:call-template name="formatDateTime"><xsl:with-param name="dateTime" select="record_info/cancel_date"/></xsl:call-template></td>
				</tr>
				<tr><td>Вид зарегистрированного вещного права: </td> <td><xsl:value-of select = "right_data/right_type/value"/></td></tr>
				<tr><td>Номер регистрации вещного права: </td> <td><xsl:value-of select = "right_data/right_number"/></td></tr>
				
				<xsl:for-each select="right_holders/right_holder">
					<xsl:choose>
						
						<xsl:when test="legal_entity">
							<tr><td colspan="2"><center><i>Правообладатель (Юридическое лицо, орган власти)</i></center></td></tr>
							<xsl:choose>
								<xsl:when test="legal_entity/entity">
									<xsl:choose>
										<xsl:when test="legal_entity/entity/resident">
											<tr><td>Российсконе юридическое лицо: </td> <td><xsl:value-of select = "legal_entity/entity/resident/name"/></td></tr>
											<tr><td>ИНН: </td> <td><xsl:value-of select = "legal_entity/entity/resident/inn"/></td></tr>    
										</xsl:when>
										<xsl:when test="legal_entity/entity/not_resident">
											<tr><td>Иностранное юридическое лицо: </td> <td><xsl:value-of select = "legal_entity/entity/not_resident/name"/></td></tr>
											<tr><td>Адрес (местонахождение) в стране регистрации (инкорпорации): </td> <td><xsl:value-of select = "legal_entity/entity/not_resident/reg_address_subject"/></td></tr>    
										</xsl:when>
									</xsl:choose>
								</xsl:when>
							</xsl:choose>
						</xsl:when>
						
						<xsl:when test="public_formation">
							<tr><td colspan="2"><center><i>Правообладатель (Публично-правовое образование)</i></center></td></tr>
							<xsl:choose>
								<xsl:when test="public_formation/public_formation_type/subject_of_rf">
									<tr><td>Субъект Российской Федерации: </td> <td><xsl:value-of select = "public_formation/public_formation_type/subject_of_rf/name/value"/></td></tr>
								</xsl:when>
								<xsl:when test="public_formation/public_formation_type/foreign_public">
									<tr><td>Иностранное государство: </td> <td><xsl:value-of select = "public_formation/public_formation_type/foreign_public/name/value"/></td></tr>
								</xsl:when>
								<xsl:when test="public_formation/public_formation_type/union_state">
									<tr><td>Союзное государство: </td> <td><xsl:value-of select = "public_formation/public_formation_type/union_state/name/value"/></td></tr>
								</xsl:when>
								<xsl:when test="public_formation/public_formation_type/russia">
									<tr><td>Российская Федерация: </td> <td><xsl:value-of select = "public_formation/public_formation_type/russia/name/value"/></td></tr>
								</xsl:when>
								<xsl:when test="public_formation/public_formation_type/municipality">
									<tr><td>Муниципальное образование: </td> <td><xsl:value-of select = "public_formation/public_formation_type/municipality/name/value"/></td></tr>
								</xsl:when>
							</xsl:choose>        
						</xsl:when>
						
						<xsl:when test="individual">
							<tr><td colspan="2" class="row_group_1"><center><i>Правообладатель (Физическое лицо)</i></center></td></tr>
							<tr><td>ФИО: </td> <td>
								<xsl:choose>
									<xsl:when test="individual/surname">
										<xsl:value-of select = "individual/surname"/><xsl:text> </xsl:text>
									</xsl:when>
								</xsl:choose>
								<xsl:choose>                                    
									<xsl:when test="individual/name">
										<xsl:value-of select = "individual/name"/><xsl:text> </xsl:text>
									</xsl:when>    
								</xsl:choose>
								<xsl:choose>                                    
									<xsl:when test="individual/patronymic">
										<xsl:value-of select = "individual/patronymic"/>
									</xsl:when>
								</xsl:choose>
							</td></tr>
						</xsl:when>
						<xsl:when test="another">
							<tr><td colspan="2"><center><i>Сведения о правообладателе (Иной субъект права)</i></center></td></tr>
						</xsl:when>
					</xsl:choose>
				</xsl:for-each>
				
			</table>
			<br/>
		</xsl:for-each>
	</xsl:template>	
	
</xsl:stylesheet>